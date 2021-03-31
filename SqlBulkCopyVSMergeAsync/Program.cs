using SqlBulkCopyVSMergeAsync.Handlers;
using System.Threading.Tasks;

namespace SqlBulkCopyVSMergeAsync
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Worker worker = new Worker();
            return await worker.RunWorkerAsync().ConfigureAwait(false);
        }
    }
}

