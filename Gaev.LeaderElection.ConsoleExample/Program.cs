using System;
using System.Configuration;

namespace Gaev.LeaderElection.ConsoleExample
{
    class Program
    {
        static void Main(string[] _)
        {
            var sqlConnectionString = ConfigurationManager.ConnectionStrings["Sql"].ConnectionString;
            var mongoConnectionString = ConfigurationManager.ConnectionStrings["Mongo"].ConnectionString;
            var app = "web";
            var node = Guid.NewGuid().ToString();

            using (var election = new MsSql.LeaderElection(sqlConnectionString))
            //using (var election = new Mutex.LeaderElection())
            //using (var election = new File.LeaderElection(@"\\192.168.1.1\gaev1tb_900\"))
            //using (var election = new MongoDb.LeaderElection(mongoConnectionString))
            {
                election.BecomeLeader(app, node, leader => Console.WriteLine(leader.AmILeader ? "MASTER" : "SLAVE"));
                Console.ReadLine();
            }
        }
    }
}
