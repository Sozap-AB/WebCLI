using CommandLine;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class InterruptableActiveCommand : CommandBase<InterruptableActiveCommand.Options>
    {
        [Verb("activeinterrupt", HelpText = "Example of active interruptable command. " +
            "Prints Fibonacci numbers with long (5 seconds) delays in between, until stopped (Ctrl + Q). " +
            "Difference from interrupt command is that we don't need to wait for long delay to finish. " +
            "May be usefull if command is making cancellable async calls.")]
        public class Options
        {
        }

        public async override IAsyncEnumerable<string> ExecuteAsync(Options options, IControlMessageQueue cmq)
        {
            using CancellationTokenSource interruptionCTS = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    while (true)
                    {
                        if (await cmq.DequeueAsync() == ControlMessage.Interrupt)
                        {
                            await interruptionCTS.CancelAsync();
                            break;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // NOTE: this catch block is needed for the case when
                    // command finishes execution before interruption message received
                }
            });

            int f0 = 0, f1 = 1;
            int index = 0;

            while (true)
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

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken: interruptionCTS.Token);
                }
                catch (OperationCanceledException)
                {
                    break; // caused by "Interrupt" control message
                }
            }

            yield return "Operation interrupted";
        }
    }
}
