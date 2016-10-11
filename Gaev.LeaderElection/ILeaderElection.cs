using System;

namespace Gaev.LeaderElection
{
    public interface ILeaderElection : IDisposable
    {
        IDisposable BecomeLeader(string app, string node, Action<Leader> onLeaderChanged);
    }
}
