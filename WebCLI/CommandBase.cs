using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebCli
{
    public abstract class CommandBase<T>
    {
        public async virtual IAsyncEnumerable<string> ExecuteAsync(T options, IControlMessageQueue cmq)
        {
            yield return await ExecuteAsync(options);
        }

        public virtual Task<string> ExecuteAsync(T options)
        {
            throw new InvalidOperationException();
        }
    }
}
