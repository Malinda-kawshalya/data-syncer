using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;
using System.IO;
using DataSyncer.Service.Services;

namespace DataSyncer.Service.Implementations
{
    public class FluentFtpTransfer : IFileTransferService
    {
        private readonly ILogger<FluentFtpTransfer> _logger;
        public FluentFtpTransfer(ILogger<FluentFtpTransfer> logger)
        {
            _logger = logger;
        }

        public async Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters)
        {
            var result = new TransferResultDto();

            var client = new FtpClient(connection.Host, connection.Port, connection.Username, connection.Password);
            client.Config.EncryptionMode = FtpEncryptionMode.None; // adjust for FTPS if needed
            try
            {
                await client.ConnectAsync();

                foreach (var f in files)
                {
                    try
                    {
                        // Simplest upload example - remote path same as filename
                        using var fs = File.OpenRead(f.FullPath);
                        var success = await client.UploadAsync(fs, Path.GetFileName(f.FullPath), FtpExists.Overwrite, true);
                        result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = f.FileName, Status = success ? "Success" : "Failed", Message = success ? "Uploaded" : "Upload failed" });
                    }
                    catch (Exception ex)
                    {
                        result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = f.FileName, Status = "Failed", Message = ex.Message });
                    }
                }

                await client.DisconnectAsync();
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection error");
                result.Success = false;
                result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = "", Status = "Failed", Message = ex.Message });
            }

            return result;
        }
    }
}
