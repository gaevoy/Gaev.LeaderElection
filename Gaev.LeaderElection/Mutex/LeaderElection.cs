using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Gaev.LeaderElection.Utils;

namespace Gaev.LeaderElection.Mutex
{
    /// <summary>
    /// Mutex-based leader election can be used only within same operating system, use it for test purpose only
    /// </summary>
    public class LeaderElection : ILeaderElection
    {
        private readonly List<IDisposable> _tasks = new List<IDisposable>();
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
            bool _;
            bool? isLeader = null;
            using (var mutex = new System.Threading.Mutex(false, "Global\\" + app, out _, GetMutexSecurity()))
                while (true)
                {
                    bool isStillLeader;
                    try
                    {
                        var firstCall = (isLeader == null);
                        isStillLeader = await mutex.WaitOneAsync(firstCall ? 50 : 5000, cancellationToken);
                    }
                    catch (AbandonedMutexException)
                    {
                        isStillLeader = true;
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    if (isLeader != isStillLeader)
                    {
                        isLeader = isStillLeader;
                        onLeaderChanged(new Leader { App = app, Node = isStillLeader ? node : null, AmILeader = isStillLeader });
                    }
                }
        }

        private static MutexSecurity GetMutexSecurity()
        {
            var securityIdentifier = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
            var allowEveryoneRule = new MutexAccessRule(securityIdentifier, MutexRights.FullControl, AccessControlType.Allow);
            var security = new MutexSecurity();
            security.AddAccessRule(allowEveryoneRule);
            return security;
        }
    }
}