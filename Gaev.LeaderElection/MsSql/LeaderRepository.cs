using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace Gaev.LeaderElection.MsSql
{
    public class LeaderRepository : ILeaderRepository
    {
        private readonly string _connectionString;
        private static readonly string EnsureTableCreatedQuery = ReadEmbeddedFile("Gaev.LeaderElection.MsSql.Sql.EnsureTableCreated.sql");
        private static readonly string SaveAndRenewQuery = ReadEmbeddedFile("Gaev.LeaderElection.MsSql.Sql.SaveAndRenew.sql");
        private static readonly string RemoveQuery = ReadEmbeddedFile("Gaev.LeaderElection.MsSql.Sql.Remove.sql");
        private static volatile bool _isTableCreated = false;

        public LeaderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }
        
        public async Task<LeaderDto> SaveAndRenewAsync(LeaderDto leader, int renewPeriodMilliseconds)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                await con.OpenAsync();
                await EnsureTableCreated(con);
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandText = SaveAndRenewQuery;
                AddParameter(cmd, "app", leader.App);
                AddParameter(cmd, "node", leader.Node);
                AddParameter(cmd, "expired", leader.ExpiredAt == default(DateTimeOffset) ? (DateTimeOffset?)null : leader.ExpiredAt);
                AddParameter(cmd, "renewperiod", renewPeriodMilliseconds);
                var reader = await cmd.ExecuteReaderAsync();
                using (reader)
                    while (reader.Read())
                    {
                        leader.Node = (string)reader["node"];
                        leader.ExpiredAt = (DateTime)reader["expired"];
                        return leader;
                    }
            }
            return null;
        }
        
        public async Task RemoveAsync(LeaderDto leader)
        {
            using (var con = new SqlConnection(_connectionString))
            {
                await con.OpenAsync();
                await EnsureTableCreated(con);
                SqlCommand cmd = con.CreateCommand();
                cmd.CommandText = RemoveQuery;
                AddParameter(cmd, "app", leader.App);
                AddParameter(cmd, "node", leader.Node);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        
        private async Task EnsureTableCreated(SqlConnection con)
        {
            if (_isTableCreated) return;
            SqlCommand cmd = con.CreateCommand();
            cmd.CommandText = EnsureTableCreatedQuery;
            await cmd.ExecuteNonQueryAsync();
            _isTableCreated = true;
        }

        private static string ReadEmbeddedFile(string fileName)
        {
            using (var stream = typeof(LeaderRepository).Assembly.GetManifestResourceStream(fileName))
            {
                stream.Position = 0;
                return new StreamReader(stream).ReadToEnd();
            }
        }

        private static void AddParameter(SqlCommand cmd, string parameterName, object value)
        {
            var parameter = cmd.CreateParameter();
            parameter.ParameterName = parameterName;
            if (value == null)
            {

            }
            else if (value is string)
                parameter.DbType = DbType.String;
            else if (value is DateTimeOffset)
            {
                value = ((DateTimeOffset)value).DateTime;
                parameter.DbType = DbType.DateTime;
            }
            else if (value is Int32)
            {
                parameter.DbType = DbType.Int32;
            }
            else
                throw new NotImplementedException();
            parameter.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(parameter);
        }
    }
}