using System.Collections.Generic;
using System.Threading.Tasks;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;

namespace DataSyncer.Core.Interfaces
{
    public interface IFileTransferService
    {
        Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters);
    }
}
