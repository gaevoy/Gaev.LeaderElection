using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Gaev.LeaderElection.Tests.Utils;
using NUnit.Framework;

namespace Gaev.LeaderElection.Tests
{
    [TestFixture("Mutex")]
    [TestFixture("File")]
    [TestFixture("MsSql")]
    [TestFixture("MongoDb")]
    public class LeaderElectionTests
    {
        private readonly string _leaderElectionType;

        public LeaderElectionTests(string leaderElectionType)
        {
            _leaderElectionType = leaderElectionType;
        }
        private readonly List<IDisposable> _nodes = new List<IDisposable>();

        [Test]
        public async Task LeaderMustBeOnlyOne()
        {
            // Given
            var app = Guid.NewGuid().ToString();
            var nodes = Enumerable.Range(1, 3).Select(i => NewNode(app)).ToList();
            await Task.WhenAll(nodes.Select(e => e.Start()));

            // When
            var leaderNodes = nodes.Where(e => e.IsLeader).ToArray();

            // Then
            Assert.AreEqual(1, leaderNodes.Length);
        }

        [Test]
        public async Task ElectionMustFindNewLeaderAfterKillingCurrentOne()
        {
            // Given
            var app = Guid.NewGuid().ToString();
            var nodes = Enumerable.Range(1, 3).Select(i => NewNode(app)).ToList();
            await Task.WhenAll(nodes.Select(e => e.Start()));
            nodes.FirstOrDefault(e => e.IsLeader)?.Kill();

            // When
            await Task.WhenAny(nodes.Select(e => e.WaitForLeader()));
            var leaderNodes = nodes.Where(e => e.IsLeader).ToArray();

            // Then
            Assert.AreEqual(1, leaderNodes.Length);
        }

        [Test]
        public async Task ElectionMustFindNewLeaderAfterLeavingOfCurrent()
        {
            // Given
            var app = Guid.NewGuid().ToString();
            var nodes = Enumerable.Range(1, 3).Select(i => NewNode(app)).ToList();
            await Task.WhenAll(nodes.Select(e => e.Start()));
            nodes.FirstOrDefault(e => e.IsLeader)?.Stop();

            // When
            await Task.WhenAny(nodes.Select(e => e.WaitForLeader()));
            var leaderNodes = nodes.Where(e => e.IsLeader).ToArray();

            // Then
            Assert.AreEqual(1, leaderNodes.Length);
        }

        [TearDown]
        public void Cleanup()
        {
            Parallel.ForEach(_nodes, node => node.Dispose());
            _nodes.Clear();
        }

        private Node NewNode(string app, string node = null)
        {
            node = node ?? Guid.NewGuid().ToString();
            var instance = new Node(_leaderElectionType, app, node);
            _nodes.Add(instance);
            return instance;
        }
    }

    public class Node : IDisposable
    {
        private readonly string _leaderElectionType;
        private readonly string _app;
        private readonly string _node;
        public bool IsLeader { get; private set; }
        private Process _process;
        private TaskCompletionSource<string> _onOutputAppeared;

        public Node(string leaderElectionType, string app, string node)
        {
            _leaderElectionType = leaderElectionType;
            _app = app;
            _node = node;
        }

        public Task Start()
        {
            _process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = typeof(Node).Assembly.Location,
                    Arguments = $"{_app} {_node} {_leaderElectionType}",
                    //                    WindowStyle = ProcessWindowStyle.Minimized,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            _process.Start();

            _onOutputAppeared = new TaskCompletionSource<string>();
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_process == null) return;
                    while (!_process.StandardOutput.EndOfStream)
                    {
                        var output = _process.StandardOutput.ReadLine();
                        IsLeader = (output == "MASTER");
                        _onOutputAppeared.SetResult(output);
                    }
                    await Task.Delay(50);
                }
            });
            return _onOutputAppeared.Task;
        }

        public async Task WaitForLeader()
        {
            while (true)
            {
                if (IsLeader) return;
                _onOutputAppeared = new TaskCompletionSource<string>();
                await _onOutputAppeared.Task;
            }
        }

        public void Kill()
        {
            IsLeader = false;
            Dispose();
        }

        public void Stop()
        {
            _process.StandardInput.WriteLine("c");
            IsLeader = false;
        }

        public void Dispose()
        {
            var process = _process;
            _process = null;
            if (process != null)
            {
                if (!process.HasExited)
                    process.Kill();
                process.Dispose();
            }
        }
    }
}
