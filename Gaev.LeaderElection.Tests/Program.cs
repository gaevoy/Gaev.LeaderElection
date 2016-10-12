using System;
using System.Configuration;

namespace Gaev.LeaderElection.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            var app = args[0];
            var node = args[1];
            var type = args[2];

            ILeaderElection election;
            switch (type)
            {
                case "Mutex":
                    election = new Mutex.LeaderElection();
                    break;
                case "File":
                    election = new File.LeaderElection();
                    break;
                case "MsSql":
                    election = new LeaderElection.MsSql.LeaderElection(ConfigurationManager.ConnectionStrings["Main"].ConnectionString);
                    break;
                default:
                    throw new NotImplementedException();
            }
            using (election)
            {
                election.BecomeLeader(app, node, leader => { Console.WriteLine(leader.AmILeader ? "MASTER" : "SLAVE"); });
                Console.ReadLine();
            }
        }
    }
}
