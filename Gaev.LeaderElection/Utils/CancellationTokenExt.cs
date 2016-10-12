using System.Threading;
using System.Threading.Tasks;

namespace Gaev.LeaderElection.Utils
{
    public static class CancellationTokenExt
    {
        public static Task AsTask(this CancellationToken cancellationToken)
        {
            var taskSource = new TaskCompletionSource<object>();
            cancellationToken.Register(() => taskSource.SetResult(null));
            return taskSource.Task;
        }
    }
}