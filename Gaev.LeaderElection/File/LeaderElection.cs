using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gaev.LeaderElection.Utils;

namespace Gaev.LeaderElection.File
{
    /// <summary>
    /// File-based leader election can be used across multiple computers within same network, use shared folder e.g. via Windows Share 
    /// </summary>
    public class LeaderElection : ILeaderElection
    {
        private readonly List<IDisposable> _tasks = new List<IDisposable>();
        private readonly string _locksDirectory;

        public LeaderElection(string locksDirectory = null)
        {
            _locksDirectory = locksDirectory ?? Path.GetTempPath();
        }
        public void Dispose()
        {
            foreach (var task in _tasks)
                task.Dispose();
            _tasks.Clear();
        }
        public IDisposable BecomeLeader(string app, string node, Action<Leader> onLeaderChanged)
        {
            var cancellationToken = new CancellationTokenSource();
            var running = BecomeLeaderAsync(cancellationToken.Token, app, node, onLeaderChanged);
            var disposableTask = new DisposableTask(running, cancellationToken);
            _tasks.Add(disposableTask);
            return disposableTask;
        }

        private async Task BecomeLeaderAsync(CancellationToken cancellationToken, string app, string node, Action<Leader> onLeaderChanged)
        {
            bool? isLeader = null;
            var lockFilePath = Path.Combine(_locksDirectory, app + ".lock");
            while (true)
            {
                try
                {
                    // http://stackoverflow.com/a/50800
                    using (var fs = new FileStream(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        byte[] dummy = new byte[100];
                        await fs.ReadAsync(dummy, 0, dummy.Length);
                        isLeader = true;
                        onLeaderChanged(new Leader { App = app, Node = node, AmILeader = true });
                        await cancellationToken.AsTask();
                        return;
                    }
                }
                catch (Exception)
                {
                    if (isLeader != false)
                    {
                        isLeader = false;
                        onLeaderChanged(new Leader { App = app, AmILeader = false });
                    }
                    if (await TaskExt.Delay(500, cancellationToken)) return;
                }
            }
        }
    }
}