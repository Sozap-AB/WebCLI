using CommandLine;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace WebCli.Internal
{
    internal class WebCliController
    {
        private readonly TimeSpan PING_PONG_EXECUTION_TIME_LIMIT = TimeSpan.FromSeconds(2);

        private WebCliOptions Options { get; }
        private IDictionary<Type, Type> OptionsCommandTypesMap { get; }
        private IServiceProvider ServiceProvider { get; }

        private Lazy<byte[]> IndexPageHtml { get; }

        internal WebCliController(WebCliOptions options, IServiceProvider serviceProvider)
        {
            Options = options;
            OptionsCommandTypesMap = options.CommandTypes.ToDictionary(GetOptionsType, x => x);
            ServiceProvider = serviceProvider;

            IndexPageHtml = new Lazy<byte[]>(RenderIndexPage);
        }

        private static Type GetOptionsType(Type commandType)
        {
            Type type = commandType;

            while (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(CommandBase<>))
                type = type.BaseType ?? throw new ArgumentException($"Command type \"{commandType}\" should derive from CommandBase");

            return type.GetGenericArguments().First();
        }

        private string ReadResourceAsString(string res)
        {
            using Stream stream = typeof(WebCliController).Assembly.GetManifestResourceStream(res)!;
            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private byte[] RenderIndexPage()
        {
            var template = ReadResourceAsString("WebCLI.Resources.index.html");

            template = template.Replace(
                "'<PARAMS>'",
                System.Text.Json.JsonSerializer.Serialize(
                    new Dictionary<string, object>()
                    {
                        { "greetings", Options.Greetings },
                        { "prompt", Options.Prompt }
                    }
                )
            );

            if (!Options.UseCDN)
            {
                var replacement = $"""
                    <script>{ReadResourceAsString("WebCLI.Resources.jquery.min.js")}</script>
                    <script>{ReadResourceAsString("WebCLI.Resources.jquery.terminal.min.js")}</script>
                    <style>{ReadResourceAsString("WebCLI.Resources.jquery.terminal.min.css")}</style>
                """;

                template = Regex.Replace(
                    template,
                    @"<!-- third_party_start -->[\s\S]+<!-- third_party_end -->",
                    replacement.Replace("$", "$$"),
                    RegexOptions.Multiline
                );
            }

            return Encoding.UTF8.GetBytes(template);
        }

        public async Task Index(HttpContext httpContext)
        {
            httpContext.Response.ContentType = "text/html";

            await httpContext.Response.BodyWriter.WriteAsync(IndexPageHtml.Value);
        }

        public async Task HandleWebSocket(WebSocket webSocket)
        {
            WebSocketReader reader = new WebSocketReader(webSocket);

            reader.RunReadingLoop();

            try
            {
                while (true)
                {
                    using CancellationTokenSource nextMessageSoftTimeoutCTS = new CancellationTokenSource();
                    if (Options.HeartbeatInterval.HasValue)
                        nextMessageSoftTimeoutCTS.CancelAfter(Options.HeartbeatInterval.Value);

                    try
                    {
                        string nextMessage = await reader.DequeueAsync(nextMessageSoftTimeoutCTS.Token);

                        var controlMessageQueue = new ControlMessageReader(reader);

                        await foreach (var output in ExecuteCommandAsync(nextMessage, controlMessageQueue))
                        {
                            if (webSocket.State != WebSocketState.Open)
                                break;

                            await webSocket.SendAsync(
                                Encoding.UTF8.GetBytes(output),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );
                        }

                        await webSocket.SendAsync(
                            Encoding.UTF8.GetBytes(Messages.SYS_MESSAGE_FINISHED),
                            WebSocketMessageType.Text,
                            true,
                            CancellationToken.None
                        );
                    }
                    catch (OperationCanceledException c)
                    {
                        if (c.CancellationToken == nextMessageSoftTimeoutCTS.Token)
                        {
                            reader.SetPongAwaiting();

                            await webSocket.SendAsync(
                                Encoding.UTF8.GetBytes(Messages.SYS_MESSAGE_PING),
                                WebSocketMessageType.Text,
                                true,
                                CancellationToken.None
                            );

                            try
                            {
                                await reader.AwaitForPongAsync(Options.HeartbeatInterval!.Value.Add(PING_PONG_EXECUTION_TIME_LIMIT));
                            }
                            catch (TimeoutException)
                            {
                                await webSocket.CloseAsync(
                                    WebSocketCloseStatus.EndpointUnavailable,
                                    "No PONG response received from client.",
                                    CancellationToken.None
                                );

                                break;
                            }
                        }
                        else
                        {
                            throw;
                        }
                    }
                }
            }
            catch (WebSocketException wse) when (wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely
            )
            {
                // websocket closed, stop processing
            }
            catch (OperationCanceledException)
            {
                // normal exit
            }
        }

        private async IAsyncEnumerable<string> ExecuteCommandAsync(string input, ControlMessageReader cmr)
        {
            IAsyncEnumerable<string>? result = null;
            using var helpWriter = new StringWriter();

            var parser = new Parser(settings =>
            {
                settings.HelpWriter = helpWriter;
            });

            using var scope = ServiceProvider.CreateScope();

            var clResult = parser.ParseArguments(SplitArguments(input), OptionsCommandTypesMap.Keys.ToArray())
                .WithParsed(delegate (object options)
                {
                    var command = scope.ServiceProvider.GetRequiredService(
                        OptionsCommandTypesMap[options.GetType()]
                    );

                    result = (IAsyncEnumerable<string>)
                        command
                            .GetType()
                            .GetMethod(
                                nameof(CommandBase<object>.ExecuteAsync),
                                [options.GetType(), typeof(ControlMessageReader)]
                            )!.Invoke(command, [options, cmr])!;

                });

            if (result != null)
            {
                await foreach (var line in result)
                {
                    yield return line;
                }

                await cmr.CompleteAsync();
            }
            else
            {
                foreach (var error in clResult.Errors)
                {
                    switch (error.Tag)
                    {
                        case ErrorType.HelpVerbRequestedError:
                            if ((error as HelpVerbRequestedError)!.Verb == null)
                            {
                                yield return FormatHelp(helpWriter.ToString());
                            }
                            else
                            {
                                yield return helpWriter.ToString();
                            }
                            break;
                        case ErrorType.VersionRequestedError:
                            yield return GetAppVersion();
                            break;
                        case ErrorType.BadVerbSelectedError:
                            yield return "Unknown command";
                            break;
                        case ErrorType.MissingRequiredOptionError:
                            yield return "Required option is missing, enter \"help COMMAND\" to see required parameters.";
                            break;
                        default:
                            yield return error.ToString()!;
                            break;
                    }
                }
            }
        }

        private string FormatHelp(string originalHelp)
        {
            var additional = """
                Shortcuts

                  CTRL+Q - interrupt current command execution.
                   
                  Up Arrow/CTRL+P — show previous command from history.
                  Down Arrow/CTRL+N — show next command from history.
                  CTRL+R — reverse Search through command line history.
                  CTRL+G — cancel Reverse Search.
                   
                  CTRL+L — clear terminal.
                   
                  CTRL+Left Arrow — move one word to the left.
                  CTRL+Right Arrow — move one word to the right.
                  CTRL+A/Home — move to beginning of the line.
                  CTRL+E/End — move to end of the line.
                   
                  CTRL+K — remove the text after the cursor and save it in kill area.
                  CTRL+U — remove the text before the cursor and save it in kill area.
                  CTRL+Y — paste text from kill area.
                   
                  Shift+Enter — insert new line.

                """;

            return originalHelp + additional;
        }

        private string GetAppVersion()
        {
            var assemblyName = Assembly.GetEntryAssembly()!.GetName();

            return $"{assemblyName.Name} {assemblyName.Version}";
        }

        private IEnumerable<string> SplitArguments(string input)
        {
            var regex = new Regex(@"[""].+?[""]|\S+", RegexOptions.Compiled);

            foreach (string value in regex.Matches(input).Select(x => x.Value))
            {
                if (value.StartsWith('\"') && value.EndsWith('\"'))
                    yield return value.Substring(1, value.Length - 2);
                else
                    yield return value;
            }
        }
    }
}
