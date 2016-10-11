using System;
using System.Threading.Tasks;

namespace Gaev.LeaderElection.MsSql
{
    public class RetryAwareLeaderRepository : ILeaderRepository
    {
        private readonly ILeaderRepository _underlying;
        private int _retryCount = 3;
        private int _sleepInterval = 100;

        public RetryAwareLeaderRepository(ILeaderRepository underlying)
        {
            _underlying = underlying;
        }

        public Task RemoveAsync(LeaderDto leader)
        {
            return RetryAsync(async () => await _underlying.RemoveAsync(leader));
        }

        public Task<LeaderDto> SaveAndRenewAsync(LeaderDto leader, int renewPeriodMilliseconds)
        {
            return RetryAsync(async () => await _underlying.SaveAndRenewAsync(leader, renewPeriodMilliseconds));
        }

        private async Task<T> RetryAsync<T>(Func<Task<T>> act)
        {
            for (int i = 1; i <= _retryCount; i++)
            {
                try
                {
                    return await act();
                }
                catch (Exception)
                {
                    if (i == _retryCount)
                        throw;
                    await Task.Delay(_sleepInterval);
                }
            }
            return default(T);
        }

        private Task RetryAsync(Func<Task> act)
        {
            return RetryAsync<object>(async () => { await act(); return null; });
        }
    }
}