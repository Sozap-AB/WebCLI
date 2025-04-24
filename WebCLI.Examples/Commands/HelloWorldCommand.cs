using CommandLine;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class HelloWorldCommand : CommandBase<HelloWorldCommand.Options>
    {
        [Verb("helloworld", HelpText = "Hellow world example. Prints hello world.")]
        public class Options
        {
        }

        public override Task<string> ExecuteAsync(Options options)
        {
            return Task.FromResult("Hello world!");
        }
    }
}
