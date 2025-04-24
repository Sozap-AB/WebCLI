using System.Threading;
using System.Threading.Tasks;

namespace WebCli
{
    public interface IControlMessageQueue
    {
        ControlMessage? TryDequeue();

        Task<ControlMessage> DequeueAsync(CancellationToken cancellationToken = default);
    }
}
