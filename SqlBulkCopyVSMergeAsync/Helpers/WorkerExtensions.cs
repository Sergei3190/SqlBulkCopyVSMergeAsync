using NLog;
using SqlBulkCopyVSMergeAsync.DataBase;
using SqlBulkCopyVSMergeAsync.Handlers;
using SqlBulkCopyVSMergeAsync.Properties;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;

namespace SqlBulkCopyVSMergeAsync.Helpers
{
    public static class WorkerExtensions
    {
        private static Logger _log = LogManager.GetLogger(nameof(WorkerExtensions));

        public static async Task<(List<int> points, List<long> requestsId, Dictionary<string, string> tablesNamesDB,
            Dictionary<string, DataTable> clientTables)> GetSourceTestDataAsync(this Worker worker)
        {
            if (worker is null)
                throw new ArgumentNullException(nameof(worker));

            var dataBuilder = new SourceTestDataBuilder();

            try
            {
                var objectsToFill = await dataBuilder.GetObjectsToFillAsync().ConfigureAwait(false);
                _log.Info("Сформированы объекты для заполнения:\r\n" +
                          "кол-во точек = {0}\r\n" +
                          "кол-во запросов = {1}\r\n" +
                          "кол-во таблиц БД = {2}\r\n",
                          (objectsToFill.points is null) ? 0 : objectsToFill.points.Count,
                          (objectsToFill.requestsId is null) ? 0 : objectsToFill.requestsId.Count,
                          (objectsToFill.tablesNamesDB is null) ? 0 : objectsToFill.tablesNamesDB.Count);

                var filledObjects = await dataBuilder.GetFilledObjectsAsync(objectsToFill.points, objectsToFill.requestsId).ConfigureAwait(false);
                _log.Info("Сформированы заполненные объекты:\r\n" +
                          "таблица {0}, кол-во строк = {1}\r\n" +
                          "таблица {2}, кол-во строк = {3}\r\n",
                          filledObjects["dtPointIdIsPK"].TableName, (filledObjects["dtPointIdIsPK"] is null) ? 0 : filledObjects["dtPointIdIsPK"].Rows.Count,
                          filledObjects["dtPointIdIsFK"].TableName, (filledObjects["dtPointIdIsFK"] is null) ? 0 : filledObjects["dtPointIdIsFK"].Rows.Count);

                return (objectsToFill.points, objectsToFill.requestsId, objectsToFill.tablesNamesDB, filledObjects);
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода GetSourceTestDataAsync\r\n{ex}");
                return (null, null, null, null);
            }
        }

        public static async Task<int> AddRequestsInDBAsync(this Worker worker, SqlCommands sqlCommands, List<long> requestsId)
        {
            if (worker is null)
                throw new ArgumentNullException(nameof(worker));

            if (sqlCommands is null)
                throw new ArgumentNullException(nameof(sqlCommands));

            if (requestsId is null)
                throw new ArgumentNullException(nameof(requestsId));

            var statusDefinition = new StringBuilder();

            try
            {
                for (int i = 0; i < requestsId.Count; i++)
                {
                    switch (i)
                    {
                        case int count when count < 3:
                            statusDefinition.Append(Settings.Default.StatusSucces);
                            break;
                        case int count when count >= 3 && count <= requestsId.Count:
                            statusDefinition.Append(Settings.Default.StatusInProgress);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(requestsId));
                    }

                    var result = await sqlCommands.AddValuesRequestsAsync(requestsId[i], statusDefinition.ToString()).ConfigureAwait(false);

                    if (result != 1)
                    {
                        _log.Info($"Не удалось добавить данные об идентификаторе запроса и его статусе в БД");
                        return -1;
                    }

                    statusDefinition.Clear();
                }

                return 1;
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода FillRequestsInDBAsync\r\n{ex}");
                return -1;
            }
        }

        public static async Task<int> TestsMethodsRunAsync(this Worker worker, SqlCommands sqlCommands, Dictionary<string, DataTable> clientTables,
             Dictionary<string, string> tablesNamesDB)
        {
            if (worker is null)
                throw new ArgumentNullException(nameof(worker));

            if (sqlCommands is null)
                throw new ArgumentNullException(nameof(sqlCommands));

            if (clientTables is null)
                throw new ArgumentNullException(nameof(clientTables));

            if (tablesNamesDB is null)
                throw new ArgumentNullException(nameof(tablesNamesDB));

            DateTime start = default;
            DateTime over = default;

            var result = -1;
            DataTable clientTable = null;

            try
            {
                foreach (var tableNameDB in tablesNamesDB)
                {
                    switch (tableNameDB.Key)
                    {
                        case "PointsToRetryMergePK":
                        case "PointsToRetryMergeFK":
                            start = DateTime.Now;
                            result = await sqlCommands.AddValuesPointsToRetryAsync(clientTables["dtPointIdIsPK"], tableNameDB.Key).ConfigureAwait(false);
                            over = DateTime.Now;
                            break;
                        case "PointsToRetrySqlBulkCopyPK":
                            clientTable = clientTables["dtPointIdIsPK"];
                            break;
                        case "PointsToRetrySqlBulkCopyFK":
                            clientTable = clientTables["dtPointIdIsFK"];
                            break;
                        default:
                            throw new ArgumentException(nameof(tablesNamesDB));
                    }

                    if (clientTable != null)
                    {
                        start = DateTime.Now;
                        result = await sqlCommands.ExecuteBulkImportPointsToRetryAsync(clientTable, tableNameDB.Key).ConfigureAwait(false);
                        over = DateTime.Now;
                    }

                    if (result <= 0)
                        return -1;

                    worker.PrintMethodResult(result, start, over, tableNameDB.Key, tableNameDB.Value);
                }

                return 1;
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода TestsMethodsRunAsync\r\n{ex}");
                return -1;
            }
        }

        public static void PrintMethodResult(this Worker worker, in int result, in DateTime start, in DateTime over, string tableName, string methodName)
        {
            if (worker is null)
                throw new ArgumentNullException(nameof(worker));

            _log.Info("Метод {0} для таблицы {1} " +
                      "отработал {2}успешно (\"{3}\")\r\n" +
                      "затрачено времени: {4}\r\n",
                      methodName, tableName, (result < 0) ? "не" : "", result, over.Subtract(start));
        }
    }
}
