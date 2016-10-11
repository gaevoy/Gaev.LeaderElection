namespace Gaev.LeaderElection
{
    public struct Leader
    {
        public string App { get; set; }
        public string Node { get; set; }
        public bool AmILeader { get; set; }
    }
}