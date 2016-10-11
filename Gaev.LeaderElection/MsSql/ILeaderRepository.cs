using System.Threading.Tasks;

namespace Gaev.LeaderElection.MsSql
{
    public interface ILeaderRepository
    {
        Task RemoveAsync(LeaderDto leader);
        Task<LeaderDto> SaveAndRenewAsync(LeaderDto leader, int renewPeriodMilliseconds);
    }
}