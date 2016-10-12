using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Gaev.LeaderElection.Tests
{
    [TestFixture("Mutex")]
    [TestFixture("File")]
    [TestFixture("MsSql")]
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
            var nodes = Enumerable.Range(1, 5).Select(i => NewNode(app)).ToList();
            await Task.WhenAll(nodes.Select(e => e.Start()));

            // When
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
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            _process.Start();

            var onStarted = new TaskCompletionSource<object>();
            Task.Run(async () =>
            {
                while (true)
                {
                    if (_process == null) return;
                    while (!_process.StandardOutput.EndOfStream)
                    {
                        IsLeader = (_process.StandardOutput.ReadLine() == "MASTER");
                        onStarted.SetResult(null);
                    }
                    await Task.Delay(50);
                }
            });
            return onStarted.Task;
        }

        public void Kill()
        {
            _process.Kill();
        }

        public void Stop()
        {
            _process.StandardInput.WriteLine("c");
        }

        public void Dispose()
        {
            var process = _process;
            _process = null;
            if (process != null)
            {
                process.Kill();
                process.Dispose();
            }
        }
    }
}
