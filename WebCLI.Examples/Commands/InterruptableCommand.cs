using CommandLine;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class InterruptableCommand : CommandBase<InterruptableCommand.Options>
    {
        [Verb("interrupt", HelpText = "Example of interruptable command. Prints Fibonacci numbers, until stopped (Ctrl + Q).")]
        public class Options
        {
        }

        public async override IAsyncEnumerable<string> ExecuteAsync(Options options, IControlMessageQueue cmq)
        {
            int f0 = 0, f1 = 1;
            int index = 0;

            while (cmq.TryDequeue() != ControlMessage.Interrupt)
            {
                switch (index)
                {
                    case 0:
                        yield return $"F{index} = {f0}";
                        break;
                    case 1:
                        yield return $"F{index} = {f1}";
                        break;
                    default:
                        var nextF = f0 + f1;

                        yield return $"F{index} = {nextF}";

                        f0 = f1;
                        f1 = nextF;

                        break;
                }

                index++;

                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            yield return "Operation interrupted";
        }
    }
}
