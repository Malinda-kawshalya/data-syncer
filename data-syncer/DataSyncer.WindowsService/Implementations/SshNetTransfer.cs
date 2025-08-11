using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;
using DataSyncer.Service.Services;

namespace DataSyncer.Service.Implementations
{
    public class SshNetTransfer : IFileTransferService
    {
        private readonly ILogger<SshNetTransfer> _logger;
        public SshNetTransfer(ILogger<SshNetTransfer> logger)
        {
            _logger = logger;
        }

        public Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters)
        {
            var result = new TransferResultDto();

            using var sftp = new Renci.SshNet.SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);
            try
            {
                sftp.Connect();
                foreach (var f in files)
                {
                    try
                    {
                        using var fs = File.OpenRead(f.FullPath);
                        sftp.UploadFile(fs, Path.GetFileName(f.FullPath));
                        result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = f.FileName, Status = "Success", Message = "Uploaded" });
                    }
                    catch (Exception ex)
                    {
                        result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = f.FileName, Status = "Failed", Message = ex.Message });
                    }
                }
                sftp.Disconnect();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP connection error");
                result.Success = false;
                result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = "", Status = "Failed", Message = ex.Message });
            }

            return Task.FromResult(result);
        }
    }
}
