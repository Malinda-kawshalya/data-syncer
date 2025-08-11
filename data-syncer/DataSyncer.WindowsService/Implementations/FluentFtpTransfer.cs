using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.Extensions.Logging;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;
using System.IO;
using DataSyncer.WindowsService.Services;

namespace DataSyncer.WindowsService.Implementations
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
            var result = new TransferResultDto { Logs = new List<TransferLog>() };

            var client = new FtpClient(connection.Host, connection.Port);
            client.Credentials = new System.Net.NetworkCredential(connection.Username, connection.Password);
            client.Config.EncryptionMode = FtpEncryptionMode.None; // adjust for FTPS if needed

            try
            {
                await Task.Run(() => client.Connect());

                foreach (var f in files)
                {
                    try
                    {
                        using var fs = File.OpenRead(f.FullPath);
                        // Use UploadFile instead of UploadAsync, and FtpRemoteExists instead of FtpExists
                        var status = await Task.Run(() =>
                            client.UploadFile(f.FullPath, Path.GetFileName(f.FullPath), FtpRemoteExists.Overwrite, true)
                        );
                        var success = status == FtpStatus.Success;
                        result.Logs.Add(new TransferLog
                        {
                            Timestamp = DateTime.Now,
                            FileName = f.FileName,
                            Status = success ? "Success" : "Failed",
                            Message = success ? "Uploaded" : "Upload failed"
                        });
                    }
                    catch (Exception ex)
                    {
                        result.Logs.Add(new TransferLog
                        {
                            Timestamp = DateTime.Now,
                            FileName = f.FileName,
                            Status = "Failed",
                            Message = ex.Message
                        });
                    }
                }

                await Task.Run(() => client.Disconnect());
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FTP connection error");
                result.Success = false;
                result.Logs.Add(new TransferLog
                {
                    Timestamp = DateTime.Now,
                    FileName = "",
                    Status = "Failed",
                    Message = ex.Message
                });
            }

            return result;
        }


        public async Task<bool> TestConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                _logger.LogInformation($"Testing FTP connection to {connection.Host}:{connection.Port}");

                var client = new FtpClient(connection.Host, connection.Port);
                client.Credentials = new System.Net.NetworkCredential(connection.Username, connection.Password);
                client.Config.EncryptionMode = connection.Protocol == ProtocolType.SFTP ? FtpEncryptionMode.Auto : FtpEncryptionMode.None;

                await Task.Run(() => client.Connect());
                
                // Test if we can list directory
                var items = await Task.Run(() => client.GetListing("/"));
                
                await Task.Run(() => client.Disconnect());
                
                _logger.LogInformation("FTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"FTP connection test failed: {ex.Message}");
                return false;
            }
        }


        public async Task<bool> TransferFileAsync(ConnectionSettings connection, string sourcePath, string destinationPath)
        {
            try
            {
                _logger.LogInformation($"Starting file transfer from {sourcePath} to {destinationPath}");

                if (!File.Exists(sourcePath))
                {
                    _logger.LogError($"Source file does not exist: {sourcePath}");
                    return false;
                }

                var client = new FtpClient(connection.Host, connection.Port);
                client.Credentials = new System.Net.NetworkCredential(connection.Username, connection.Password);
                client.Config.EncryptionMode = connection.Protocol == ProtocolType.SFTP ? FtpEncryptionMode.Auto : FtpEncryptionMode.None;

                await Task.Run(() => client.Connect());
                
                // Get just the filename for the remote path
                var fileName = Path.GetFileName(sourcePath);
                var remotePath = destinationPath.EndsWith("/") ? destinationPath + fileName : destinationPath + "/" + fileName;
                
                _logger.LogInformation($"Uploading {sourcePath} to {remotePath}");
                
                var status = await Task.Run(() =>
                    client.UploadFile(sourcePath, remotePath, FtpRemoteExists.Overwrite, true)
                );
                
                await Task.Run(() => client.Disconnect());
                
                var success = status == FtpStatus.Success;
                _logger.LogInformation($"File transfer {(success ? "completed successfully" : "failed")}");
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"File transfer failed: {ex.Message}");
                return false;
            }
        }
    }
}
