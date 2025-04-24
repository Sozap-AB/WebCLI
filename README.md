WebCLI is a lightweight library for building command line UI in a web browser for AspNetCore web application. Good for making administration panels without frontend development.

# Features #

  * Easy to configure
    * No frontend development required
    * Create command, register it as service and pass to "UseWebCli" and you're good to go
  * Based on jQuery Terminal library, it includes its usability features (history in browser, text navigation, hot keys, etc.) 
  * Provide intermediate output during complex commands execution (see example [AsyncMultistepOperationCommand](WebCLI.Examples/Commands/AsyncMultistepOperationCommand.cs))
  * Replace previous output to refresh provided information (see example [ProgressBarCommand](WebCLI.Examples/Commands/ProgressBarCommand.cs))

# Pre Requirements #

Can be installed into web application based on ASP.NET Core 8.0+

# Getting started #

1. Install NuGet package:
    ```sh
    Install-Package WebCLI
    ```
2. Create some command:
    ```csharp
    namespace App.Commands
    {
        public class HelloWorldCommand : CommandBase<HelloWorldCommand.Options>
        {
            [Verb("helloworld")]
            public class Options
            {
            }

            public override Task<string> ExecuteAsync(Options options)
            {
                return Task.FromResult("Hello world!");
            }
        }
    }
    ```
3. Register command in IServiceCollection:
    ```csharp
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddTransient<HelloWorldCommand>();

    var app = builder.Build();
    ```
4. Enable WebCLI, by calling "UseWebCli" with options:
    ```csharp
    var app = builder.Build();

    app.UseWebCli(new WebCliOptions(
        [typeof(HelloWorldCommand)]
    ));

    await app.RunAsync();
    ```
5. Run application, open `<app url> + /webcli` in browser (e.g. `http://localhost:5275/webcli`) and enter `helloworld` to see the result

# Examples #

For command & configuration examples take a look at [WebCLI.Examples](WebCLI.Examples) project. It contains:
  * [Installation](WebCLI.Examples/Program.cs) - shows how to connect WebCLI to your application
  * [HelloWorldCommand](WebCLI.Examples/Commands/HelloWorldCommand.cs) - minimal required command configuration
  * [AsyncOperationCommand](WebCLI.Examples/Commands/AsyncOperationCommand.cs) - command that performs async operation
  * [AsyncMultistepOperationCommand](WebCLI.Examples/Commands/AsyncMultistepOperationCommand.cs) - command with intermediate output (doing some async work, printing output, doing more async work, printing more output, etc.)
  * [ProgressBarCommand](WebCLI.Examples/Commands/ProgressBarCommand.cs) - command with output rewriting
  * [InterruptableCommand](WebCLI.Examples/Commands/InterruptableCommand.cs) - command with operation, that can check on certain steps if user wants to cancel it
  * [InterruptableActiveCommand](WebCLI.Examples/Commands/InterruptableActiveCommand.cs) - command with operation, that uses cancellation token (which can be used with various async APIs) indicating that user wants to cancel it
