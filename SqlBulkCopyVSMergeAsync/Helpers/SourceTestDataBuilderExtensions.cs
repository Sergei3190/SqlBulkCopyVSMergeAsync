using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace SqlBulkCopyVSMergeAsync.Helpers
{
    public static class SourceTestDataBuilderExtensions
    {
        private static readonly Logger _log = LogManager.GetLogger(nameof(SourceTestDataBuilderExtensions));

        public static async Task<(List<int> points, List<long> requestsId, Dictionary<string, string> tablesNamesDB)> GetObjectsToFillAsync(this SourceTestDataBuilder builder)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            var points = Task.Run(() => builder.GetPoints());
            var requestsId = Task.Run(() => builder.GetRequestsId());
            var tableNamesDB = Task.Run(() => builder.GetTablesNamesDB());

            var objectsToFill = Task.WhenAll(points, requestsId, tableNamesDB);
            await objectsToFill;

            return (points.Result, requestsId.Result, tableNamesDB.Result);
        }

        public static async Task<Dictionary<string, DataTable>> GetFilledObjectsAsync(this SourceTestDataBuilder builder, List<int> points, List<long> requestsId)
        {
            if (builder is null)
                throw new ArgumentNullException(nameof(builder));

            if (points is null)
                throw new ArgumentNullException(nameof(points));

            if (requestsId is null)
                throw new ArgumentNullException(nameof(requestsId));

            return await Task.Run(() => builder.GetCompletedClientTables(points, requestsId)).ConfigureAwait(false);
        }
    }
}
