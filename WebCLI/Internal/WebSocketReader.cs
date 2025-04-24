using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace WebCli.Internal
{
    internal class WebSocketReader
    {
        private const int READING_BUFFER_SIZE = 1024 * 4;

        public WebSocket WebSocket { get; }
        private Channel<string> MessageChannel { get; } = Channel.CreateUnbounded<string>();
        private TaskCompletionSource<bool>? PongReceivedTCS { get; set; } = null;

        public WebSocketReader(WebSocket webSocket)
        {
            WebSocket = webSocket;
        }

        public void RunReadingLoop()
        {
            async Task RunInternal()
            {
                var buffer = new byte[READING_BUFFER_SIZE];
                StringBuilder sb = new StringBuilder();

                try
                {
                    while (true)
                    {
                        WebSocketReceiveResult result = await WebSocket.ReceiveAsync(
                            new ArraySegment<byte>(buffer),
                            CancellationToken.None
                        );

                        if (result.CloseStatus.HasValue)
                            break;

                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                        if (result.EndOfMessage)
                        {
                            var message = sb.ToString();

                            switch (message)
                            {
                                case Messages.SYS_MESSAGE_PONG:
                                    PongReceivedTCS?.SetResult(true);
                                    break;
                                default:
                                    await MessageChannel.Writer.WriteAsync(message);
                                    break;
                            }

                            sb.Clear();
                        }
                    }
                }
                catch (WebSocketException wse) when (wse.WebSocketErrorCode == WebSocketError.ConnectionClosedPrematurely)
                {
                    // connection has been closed, reading is over
                }

                MessageChannel.Writer.Complete();
            }

            _ = Task.Run(RunInternal);
        }

        public async Task<string> DequeueAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await MessageChannel.Reader.ReadAsync(cancellationToken);
            }
            catch (ChannelClosedException)
            {
                throw new OperationCanceledException();
            }
        }

        public string? TryDequeue()
        {
            if (MessageChannel.Reader.TryRead(out string? res))
                return res;

            return null;
        }

        public void SetPongAwaiting()
        {
            PongReceivedTCS = new TaskCompletionSource<bool>();
        }

        public async Task AwaitForPongAsync(TimeSpan timeout)
        {
            var task = PongReceivedTCS!.Task;

            var timeoutTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, timeoutTask);

            if (completedTask != task)
                throw new TimeoutException();
        }
    }
}
