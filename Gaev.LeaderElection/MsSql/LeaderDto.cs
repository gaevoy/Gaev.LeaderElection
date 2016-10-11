using System;

namespace Gaev.LeaderElection.MsSql
{
    public class LeaderDto
    {
        public string App { get; set; }
        public string Node { get; set; }
        public DateTimeOffset ExpiredAt { get; set; }
    }
}