using CommandLine;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class AsyncMultistepOperationCommand : CommandBase<AsyncMultistepOperationCommand.Options>
    {
        [Verb("asyncmsop", HelpText = "Async operation with intermediate output example. Prints results of 3 arithmetic operations with delay.")]
        public class Options
        {
            [Value(1, HelpText = "Parameter A", Required = true)]
            public int A { get; }

            [Value(2, HelpText = "Parameter B", Required = true)]
            public int B { get; }

            public Options(int a, int b)
            {
                A = a;
                B = b;
            }
        }

        public async override IAsyncEnumerable<string> ExecuteAsync(Options options, IControlMessageQueue cmq)
        {
            yield return "Starting...";

            await Task.Delay(TimeSpan.FromSeconds(1));

            yield return $"A + B = {options.A + options.B}";

            yield return "Processing...";

            await Task.Delay(TimeSpan.FromSeconds(1));

            yield return $"A * B = {options.A * options.B}";

            yield return "Processing...";

            await Task.Delay(TimeSpan.FromSeconds(1));

            yield return $"A - B = {options.A - options.B}";

            yield return "Done";
        }
    }
}
