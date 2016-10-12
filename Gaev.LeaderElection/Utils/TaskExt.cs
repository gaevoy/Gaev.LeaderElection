using System;
using System.Threading;
using System.Threading.Tasks;

namespace Gaev.LeaderElection.Utils
{
    public static class TaskExt
    {
        public static async Task<bool> Delay(int millisecondsDelay, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(millisecondsDelay, cancellationToken);
                return false;
            }
            catch (OperationCanceledException)
            {
                return true;
            }
        }

        public static async Task RunAndIgnoreException(Func<Task> act)
        {
            try
            {
                await act();
            }
            catch (Exception)
            {
                // Ignore it
            }
        }
    }
}