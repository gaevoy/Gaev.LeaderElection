using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gaev.LeaderElection.Utils;

namespace Gaev.LeaderElection.MsSql
{
    /// <summary>
    /// MsSql-based leader election can be used across multiple computers and different networks, use globally accessible Sql Server instance e.g. Sql Azure
    /// </summary>
    public class LeaderElection : ILeaderElection
    {
        private readonly int _renewPeriodMilliseconds;
        private readonly int _expirationPeriodMilliseconds;
        private readonly ILeaderRepository _leaderRepository;
        private readonly List<IDisposable> _tasks = new List<IDisposable>();

        public LeaderElection(string connectionString, int renewPeriodMilliseconds = 500, int expirationPeriodMilliseconds = 1500)
        {
            _renewPeriodMilliseconds = renewPeriodMilliseconds;
            _expirationPeriodMilliseconds = expirationPeriodMilliseconds;
            _leaderRepository = new RetryAwareLeaderRepository(new LeaderRepository(connectionString));
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
            if (cancellationToken.IsCancellationRequested) return;

            string prevLeaderNode = null;
            LeaderDto leader = new LeaderDto { App = app, Node = node };
            while (true)
            {
                try
                {
                    leader.Node = node;
                    leader = await _leaderRepository.SaveAndRenewAsync(leader, _expirationPeriodMilliseconds);
                }
                catch (Exception)
                {
                    leader.Node = null; // Set to null because it is unknown whether it is still leader.
                }

                if (prevLeaderNode != leader.Node)
                {
                    prevLeaderNode = leader.Node;
                    onLeaderChanged(new Leader { App = app, Node = leader.Node, AmILeader = node == leader.Node });
                }
                if (await TaskExt.Delay(_renewPeriodMilliseconds, cancellationToken))
                {
                    await TaskExt.RunAndIgnoreException(() => _leaderRepository.RemoveAsync(leader));
                    return;
                }
            }
        }
    }
}
