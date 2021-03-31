using NLog;
using SqlBulkCopyVSMergeAsync.DataBase;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace SqlBulkCopyVSMergeAsync.Helpers
{
    public class SourceTestDataBuilder
    {
        private readonly Logger _log;
        private readonly Random _random;

        public SourceTestDataBuilder()
        {
            _log = LogManager.GetLogger(nameof(SourceTestDataBuilder));
            _random = new Random();
        }

        public List<int> GetPoints()
        {
            var points = new List<int>();

            for (int i = 1; i <= 100_000; i++)
            {
                points.Add(_random.Next(1, 100_001));
            }

            return points.Distinct<int>().ToList();
        }

        public List<long> GetRequestsId()
        {
            var requetsId = new List<long>();

            try
            {
                for (int i = 0; i < 7; i++)
                {
                    requetsId.Add(Convert.ToInt64(DateTime.Now.AddMinutes(i).ToString("yMdHms", CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Ошибка при выполнении метода GetRequestsId\r\n{ex}");
                new List<long>();
            }

            return requetsId;
        }

        public Dictionary<string, DataTable> GetCompletedClientTables(in List<int> points, in List<long> requestsId)
        {
            ClientTablesBuilder builder = new ClientTablesBuilder();

            var clientTables = builder.GetEmptyClientTables();

            foreach (var item in clientTables)
            {
                var result = builder.FillClientTables(item.Value, points, requestsId);

                if (result != 1)
                {
                    _log.Info($"Данных для заполнения таблицы массового копирования нет");
                    return null;
                }
            }

            return clientTables;
        }

        public Dictionary<string, string> GetTablesNamesDB()
        {
            return new Dictionary<string, string>()
            {
                {"PointsToRetryMergePK", "AddValuesPointsToRetryPK"},
                {"PointsToRetryMergeFK", "AddValuesPointsToRetryFK"},
                {"PointsToRetrySqlBulkCopyPK", "ExecuteBulkImportPointsToRetry" },
                {"PointsToRetrySqlBulkCopyFK", "ExecuteBulkImportPointsToRetry" }
            };
        }
    }
}
