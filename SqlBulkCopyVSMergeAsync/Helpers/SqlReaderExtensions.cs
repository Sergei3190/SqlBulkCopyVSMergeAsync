using System;
using System.Data;

namespace SqlBulkCopyVSMergeAsync.Helpers
{
    public static class SqlReaderExtensions
    {
        public static T GetValueOrDefault<T>(this IDataRecord dataRecord, string fieldName)
        {
            if (dataRecord is null)
                throw new ArgumentNullException(nameof(dataRecord));

            return dataRecord.GetValueOrDefault<T>(dataRecord.GetOrdinal(fieldName));
        }

        public static T GetValueOrDefault<T>(this IDataRecord dataRecord, int ordinal)
        {
            if (dataRecord is null)
                throw new ArgumentNullException(nameof(dataRecord));

            return (T)(dataRecord.IsDBNull(ordinal) ? default(T) : dataRecord.GetValue(ordinal));
        }
    }
}
