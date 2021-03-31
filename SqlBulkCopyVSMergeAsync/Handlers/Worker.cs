using NLog;
using SqlBulkCopyVSMergeAsync.DataBase;
using SqlBulkCopyVSMergeAsync.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SqlBulkCopyVSMergeAsync.Handlers
{
    public class Worker
    {
        private readonly Logger _log;
        private readonly SqlCommands _sqlCommands;

        public Worker()
        {
            _log = LogManager.GetLogger(nameof(Worker));
            _sqlCommands = new SqlCommands();
        }

        public async Task<int> RunWorkerAsync()
        {
            try
            {
                var sourceTestData = await this.GetSourceTestDataAsync().ConfigureAwait(false);

                var result = await this.AddRequestsInDBAsync(_sqlCommands, sourceTestData.requestsId).ConfigureAwait(false);

                if (result != 1)
                    return -1;

                var testsResult = await this.TestsMethodsRunAsync(_sqlCommands, sourceTestData.clientTables, sourceTestData.tablesNamesDB).ConfigureAwait(false);

                if (testsResult != 1)
                    return -1;

                var pointsToretry = await _sqlCommands.GetPointsToRetryAsync().ConfigureAwait(false);
                _log.Info($"Количество возвращенных из БД точек = {pointsToretry?.ToList().Count ?? 0}\r\n");

                if (pointsToretry is null)
                    return -1;

                return 1;
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода RunWorkerAsync\r\n{ex}");
                return -1;
            }
        }
    }
}
