using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gaev.LeaderElection.Utils
{
    public class DisposableTask : IDisposable
    {
        private readonly Task _task;
        private readonly CancellationTokenSource _cancellationToken;

        public DisposableTask(Task task, CancellationTokenSource cancellationToken)
        {
            _task = task;
            _cancellationToken = cancellationToken;
        }

        public void Dispose()
        {
            _cancellationToken.Cancel();
            _task.Wait();
        }
    }
}