using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Gaev.LeaderElection.Utils;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Gaev.LeaderElection.MongoDb
{
    public class LeaderElection : ILeaderElection
    {
        private readonly int _renewPeriodMilliseconds;
        private readonly int _expirationPeriodMilliseconds;
        private readonly List<IDisposable> _tasks = new List<IDisposable>();
        private readonly IMongoCollection<LockDto> _locks;
        private static volatile bool _isIndexCreated = false;

        public LeaderElection(string connectionString, int renewPeriodMilliseconds = 500, int expirationPeriodMilliseconds = 1500)
        {
            _renewPeriodMilliseconds = renewPeriodMilliseconds;
            _expirationPeriodMilliseconds = expirationPeriodMilliseconds;
            _locks = GetDatabase(connectionString).GetCollection<LockDto>("Leaders", new MongoCollectionSettings { WriteConcern = WriteConcern.WMajority });
        }

        private async Task EnsureIndexCreated(int expirationPeriodMilliseconds)
        {
            if (_isIndexCreated) return;
            var options = new CreateIndexOptions<LockDto>
            {
                ExpireAfter = TimeSpan.FromMilliseconds(expirationPeriodMilliseconds)
            };
            try
            {
                await _locks.Indexes.CreateOneAsync(Builders<LockDto>.IndexKeys.Ascending(e => e.TimeStamp), options);
            }
            catch (Exception)
            {
            }
            _isIndexCreated = true;
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
            await EnsureIndexCreated(_expirationPeriodMilliseconds);
            bool? isLeader = null;
            string prevLeaderNode = null;
            while (true)
            {
                string leaderNode;
                try
                {
                    leaderNode = await AcquireLockAndReturnOwner(app, node);
                }
                catch (Exception)
                {
                    leaderNode = null; // Set to null because it is unknown whether it is still leader.
                }
                bool isStillLeader = leaderNode == node;
                if (isLeader != isStillLeader || prevLeaderNode != leaderNode)
                {
                    isLeader = isStillLeader;
                    prevLeaderNode = leaderNode;
                    onLeaderChanged(new Leader { App = app, Node = leaderNode, AmILeader = isStillLeader });
                }
                if (await TaskExt.Delay(_renewPeriodMilliseconds, cancellationToken))
                {
                    await TaskExt.RunAndIgnoreException(() => ReleaseLock(app, node));
                    return;
                }
            }
        }

        public async Task<string> AcquireLockAndReturnOwner(string app, string node)
        {
            var update = Builders<LockDto>.Update.CurrentDate(x => x.TimeStamp);
            var result = await _locks.UpdateOneAsync(e => e.App == app && e.Node == node, update, new UpdateOptions { IsUpsert = true });
            if (result.ModifiedCount == 1 || result.UpsertedId != null)
            {
                return node;
            }
            return null; // todo fetch current node
        }

        public async Task ReleaseLock(string app, string node)
        {
            await _locks.DeleteOneAsync(e => e.App == app && e.Node == node);
        }

        private static IMongoDatabase GetDatabase(string connectionString)
        {
            return new MongoClient(connectionString).GetDatabase(new MongoUrl(connectionString).DatabaseName);
        }

        internal class LockDto
        {
            [BsonId]
            public string App { get; set; }
            public string Node { get; set; }
            public DateTime TimeStamp { get; set; }
        }
    }
}
