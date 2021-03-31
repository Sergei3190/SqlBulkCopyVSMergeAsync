using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace SqlBulkCopyVSMergeAsync.DataBase
{
    public class ClientTablesBuilder
    {
        private readonly Logger _log;
        private readonly Random _random;

        public ClientTablesBuilder()
        {
            _log = LogManager.GetLogger(nameof(ClientTablesBuilder));
            _random = new Random();
        }

        public Dictionary<string, DataTable> GetEmptyClientTables()
        {
            return new Dictionary<string, DataTable>()
            {
                { "dtPointIdIsPK", GetClientDataTableForInsertUpdate() },
                { "dtPointIdIsFK",  GetClientDataTableForInsert() }
            };
        }

        public int FillClientTables(DataTable dataTable, in List<int> points, in List<long> requestsId)
        {
            DataRow row = null;

            foreach (var item in points)
            {
                row = dataTable.NewRow();

                int index = _random.Next(requestsId.ToList().Count - 1);

                row["PointId"] = item;
                row["RequestId"] = requestsId[index];

                dataTable.Rows.Add(row);
            }

            if (dataTable.Rows.Count != 0)
                return 1;

            return -1;
        }

        private DataTable GetClientDataTableForInsertUpdate()
        {
            var dataTable = new DataTable("PointsIdPK");

            dataTable.Columns.AddRange(new DataColumn[]
                                           { new DataColumn("PointId", typeof(int)),
                                               new DataColumn("RequestId", typeof(long)) });

            return dataTable;
        }

        private DataTable GetClientDataTableForInsert()
        {
            var dataTable = new DataTable("PointsIdFK");

            dataTable.Columns.AddRange(new DataColumn[]
                                           { new DataColumn("Id", typeof(int)),
                                               new DataColumn("PointId", typeof(int)),
                                                 new DataColumn("RequestId", typeof(long))});

            return dataTable;
        }
    }
}
