using NLog;
using SqlBulkCopyVSMergeAsync.Handlers;
using SqlBulkCopyVSMergeAsync.Helpers;
using SqlBulkCopyVSMergeAsync.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace SqlBulkCopyVSMergeAsync.DataBase
{
    public class SqlCommands
    {
        private readonly Logger _log;
        private DateTime _start;
        private DateTime _over;

        public SqlCommands()
        {
            _log = LogManager.GetLogger(nameof(SqlCommands));
        }

        public async Task<int> AddValuesRequestsAsync(long requestId, string statusDefinition)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Settings.Default.ConnectionString))
                using (SqlCommand sqlCommand = new SqlCommand("[Test].[dbo].[AddValuesRequests]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    sqlCommand.Parameters.AddWithValue("@RequestId", requestId);
                    sqlCommand.Parameters.AddWithValue("@StatusDefinition", statusDefinition);

                    await sqlConnection.OpenAsync().ConfigureAwait(false);

                    return await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода AddValuesRequestsAsync\r\n{ex}");
                return -1;
            }
        }

        public async Task<int> AddValuesPointsToRetryAsync(DataTable dataTable, string tableNameDB)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Settings.Default.ConnectionString))
                using (SqlCommand sqlCommand = new SqlCommand(GetCommand(tableNameDB), sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    sqlCommand.Parameters.AddWithValue("@PointsToRetry", dataTable);

                    await sqlConnection.OpenAsync().ConfigureAwait(false);

                    return await sqlCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода AddValuesPointsToRetryAsync\r\n{ex}");
                return -1;
            }
        }

        public async Task<int> ExecuteBulkImportPointsToRetryAsync(DataTable dataTable, string tableNameDB)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Settings.Default.ConnectionString))
                {
                    SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(sqlConnection.ConnectionString, SqlBulkCopyOptions.FireTriggers)
                    {
                        DestinationTableName = tableNameDB
                    };

                    await sqlBulkCopy.WriteToServerAsync(dataTable).ConfigureAwait(false);

                    return 1;
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода ExecuteBulkImportPointsToRetryAsync\r\n{ex}");
                return -1;
            }
        }

        public async Task<IEnumerable<int>> GetPointsToRetryAsync()
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Settings.Default.ConnectionString))
                using (SqlCommand sqlCommand = new SqlCommand("[Test].[dbo].[GetPointsForRetry]", sqlConnection))
                {
                    sqlCommand.CommandType = CommandType.StoredProcedure;

                    await sqlConnection.OpenAsync().ConfigureAwait(false);

                    using (SqlDataReader sqlDataReader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        var points = new List<int>();

                        while (await sqlDataReader.ReadAsync().ConfigureAwait(false))
                        {
                            points.Add(sqlDataReader.GetValueOrDefault<int>("PointId"));
                        }

                        return points;
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода GetPointsToRetryAsync\r\n{ex}");
                return null;
            }
        }

        private string GetCommand(string tableNameDB)
        {
            switch (tableNameDB)
            {
                case "PointsToRetryMergePK":
                    return "PointsToRetryMergePK_proc";
                case "PointsToRetryMergeFK":
                    return "PointsToRetryMergeFK_proc";
                default:
                    return null;
            }
        }
    }
}
