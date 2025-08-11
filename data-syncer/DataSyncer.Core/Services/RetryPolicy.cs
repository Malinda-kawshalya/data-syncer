using System;
using System.Threading.Tasks;

namespace DataSyncer.Core.Services
{
    public class RetryPolicy
    {
        private readonly int maxRetries;
        private readonly TimeSpan delay;

        public RetryPolicy(int maxRetries = 3, TimeSpan? delay = null)
        {
            this.maxRetries = maxRetries;
            this.delay = delay ?? TimeSpan.FromSeconds(2);
        }

        public async Task ExecuteAsync(Func<Task> action)
        {
            int attempt = 0;
            while (true)
            {
                try
                {
                    attempt++;
                    await action();
                    break;
                }
                catch when (attempt < maxRetries)
                {
                    await Task.Delay(delay);
                }
            }
        }
    }
}
