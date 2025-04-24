using CommandLine;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class AsyncOperationCommand : CommandBase<AsyncOperationCommand.Options>
    {
        [Verb("asyncop", HelpText = "Async operation example. Prints sum of two numbers with delay.")]
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

        public async override Task<string> ExecuteAsync(Options options)
        {
            var result = options.A + options.B;

            await Task.Delay(TimeSpan.FromSeconds(3)); // simulating time consuming work

            return $"A + B = {result}";
        }
    }
}
