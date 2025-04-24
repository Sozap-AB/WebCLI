using CommandLine;
using System.Text;
using WebCli;

namespace WebCLI.Examples.Commands
{
    public class ProgressBarCommand : CommandBase<ProgressBarCommand.Options>
    {
        [Verb("progressbar", HelpText = "Progress bar example. Shows how to use carriage return to rewrite existing output.")]
        public class Options
        {
        }

        public async override IAsyncEnumerable<string> ExecuteAsync(Options options, IControlMessageQueue cmq)
        {
            string FormatProgressBar(int percent)
            {
                const int strLength = 64;

                StringBuilder output = new StringBuilder(new string('#', strLength * percent / 100).PadRight(64, '-'));

                var percentPart = $" {percent}% ";

                for (int i = 0; i < percentPart.Length; i++)
                    output[i + 3] = percentPart[i];

                return $"[{output.ToString()}]";
            }

            yield return "Processing...";

            for (int i = 0; i <= 100; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(0.2)); // simulating some work

                yield return (i != 0 ? '\r' : "") + FormatProgressBar(i);
            }

            yield return "Finished!";
        }
    }
}
