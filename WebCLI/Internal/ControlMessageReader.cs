using System.Threading;
using System.Threading.Tasks;

namespace WebCli.Internal
{
    internal class ControlMessageReader : IControlMessageQueue
    {
        private WebSocketReader Reader { get; }
        private CancellationTokenSource CompleteCTS { get; } = new CancellationTokenSource();

        public ControlMessageReader(WebSocketReader r)
        {
            Reader = r;
        }

        public ControlMessage? TryDequeue()
        {
            var controlMessage = Reader.TryDequeue();

            if (controlMessage == null)
                return null;

            return ParseControlMessage(controlMessage);
        }

        public async Task<ControlMessage> DequeueAsync(CancellationToken cancellationToken = default)
        {
            CancellationToken CreateCancellationToken()
            {
                if (cancellationToken == default)
                    return CompleteCTS.Token;

                return CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, CompleteCTS.Token).Token;
            }

            while (true)
            {
                var controlMessage = await Reader.DequeueAsync(CreateCancellationToken());

                if (controlMessage != null)
                {
                    var res = ParseControlMessage(controlMessage);

                    if (res != null)
                        return res.Value;
                }
            }
        }

        public async Task CompleteAsync()
        {
            await CompleteCTS.CancelAsync();

            CompleteCTS.Dispose();
        }

        private ControlMessage? ParseControlMessage(string source)
        {
            switch (source)
            {
                case Messages.CONTROL_MESSAGE_INTERRUPT:
                    return ControlMessage.Interrupt;
            }

            return null;
        }
    }
}
