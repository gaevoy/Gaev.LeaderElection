using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Gaev.LeaderElection.MsSql;
using NUnit.Framework;

namespace Gaev.LeaderElection.Tests.MsSql
{
    [TestFixture]
    public class LeaderRepositoryTests
    {
        [Test]
        public async Task LeaderCanBeChanged()
        {
            // Given
            var repo = new LeaderRepository(ConnectionString);
            var app = Guid.NewGuid().ToString();

            // When
            var leader1 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);
            await Task.Delay(50);
            var leader2 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "2" }, 200);
            await Task.Delay(150);
            var leader3 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "2" }, 200);

            // Then
            Assert.AreEqual("1", leader1.Node);
            Assert.AreEqual("1", leader2.Node);
            Assert.AreEqual("2", leader3.Node);
        }

        [Test]
        public async Task LeaderCanBeRenewed()
        {
            // Given
            var repo = new LeaderRepository(ConnectionString);
            var app = Guid.NewGuid().ToString();

            // When
            var leader1 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);
            await Task.Delay(100);
            var leader2 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);
            await Task.Delay(100);
            var leader3 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "2" }, 200);

            // Then
            Assert.AreEqual("1", leader1.Node);
            Assert.AreEqual("1", leader2.Node);
            Assert.AreEqual("1", leader3.Node);
        }

        [Test]
        public async Task LeaderCanBeRenewedAfterExpiration()
        {
            // Given
            var repo = new LeaderRepository(ConnectionString);
            var app = Guid.NewGuid().ToString();

            // When
            var leader1 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);
            await Task.Delay(250);
            var leader2 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);

            // Then
            Assert.AreEqual("1", leader1.Node);
            Assert.AreEqual("1", leader2.Node);
        }

        [Test]
        public async Task LeaderCanBeChangedJustAfterCurrentWasDeleted()
        {
            // Given
            var repo = new LeaderRepository(ConnectionString);
            var app = Guid.NewGuid().ToString();

            // When
            var leader1 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "1" }, 200);
            await repo.RemoveAsync(leader1);
            var leader2 = await repo.SaveAndRenewAsync(new LeaderDto { App = app, Node = "2" }, 200);

            // Then
            Assert.AreEqual("2", leader2.Node);
        }

        private static string ConnectionString => ConfigurationManager.ConnectionStrings["Main"].ConnectionString;
    }
}
