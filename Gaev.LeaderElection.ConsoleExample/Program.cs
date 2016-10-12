using System;
using System.Configuration;

namespace Gaev.LeaderElection.ConsoleExample
{
    class Program
    {
        static void Main(string[] _)
        {
            var connectionString = ConfigurationManager.ConnectionStrings["Main"].ConnectionString;
            var app = "web";
            var node = Guid.NewGuid().ToString();

            //using (var election = new MsSql.LeaderElection(connectionString))
            //using (var election = new Mutex.LeaderElection())
            using (var election = new File.LeaderElection())
            {
                election.BecomeLeader(app, node, leader =>
                {
                    Console.WriteLine(leader.AmILeader ? "I am master." : $"I am slave. Leader is {leader.Node}");
                });
                Console.ReadLine();
            }
        }
    }
}
