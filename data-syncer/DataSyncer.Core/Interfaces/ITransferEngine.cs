using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.Services
{
    public interface ITransferEngine
    {
        Task<TransferResult> TransferFilesAsync(TransferSettings transferSettings, List<FileInfo> files);
        Task<bool> TestConnectionAsync();
    }
}