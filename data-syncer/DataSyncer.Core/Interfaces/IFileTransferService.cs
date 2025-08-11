using System.Collections.Generic;
using System.Threading.Tasks;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;

namespace DataSyncer.Core.Interfaces
{
    public interface IFileTransferService
    {
        Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters);
        
        /// <summary>
        /// Tests the connection to the remote server
        /// </summary>
        Task<bool> TestConnectionAsync(ConnectionSettings connection);
        
        /// <summary>
        /// Transfers a single file from source to destination
        /// </summary>
        Task<bool> TransferFileAsync(ConnectionSettings connection, string sourcePath, string destinationPath);
    }
}
