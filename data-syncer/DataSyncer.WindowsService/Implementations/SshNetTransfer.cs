using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;
using DataSyncer.WindowsService.Services;

namespace DataSyncer.WindowsService.Implementations
{
    public class SshNetTransfer : IFileTransferService
    {
        private readonly ILogger<SshNetTransfer> _logger;
        public SshNetTransfer(ILogger<SshNetTransfer> logger)
        {
            _logger = logger;
        }

        public async Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters)
        {
            var result = new TransferResultDto { Logs = new List<TransferLog>() };

            using var sftp = new Renci.SshNet.SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);
            try
            {
                await Task.Run(() => sftp.Connect());
                
                foreach (var f in files)
                {
                    try
                    {
                        using var fs = File.OpenRead(f.FullPath);
                        // Create the correct remote path by combining destination path with filename
                        string remotePath = connection.DestinationPath?.TrimEnd('/') + "/" + Path.GetFileName(f.FullPath);
                        _logger.LogInformation($"Uploading {f.FullPath} to {remotePath}");
                        
                        await Task.Run(() => sftp.UploadFile(fs, remotePath, true));
                        
                        result.Logs.Add(new TransferLog 
                        { 
                            Timestamp = DateTime.Now, 
                            FileName = f.FileName, 
                            Status = "Success", 
                            Message = $"Uploaded to {remotePath}" 
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
                
                await Task.Run(() => sftp.Disconnect());
                result.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SFTP connection error");
                result.Success = false;
                result.Logs.Add(new TransferLog { Timestamp = DateTime.Now, FileName = "", Status = "Failed", Message = ex.Message });
            }

            return result;
        }

        public async Task<bool> TestConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                _logger.LogInformation($"Testing SFTP connection to {connection.Host}:{connection.Port}");

                using var client = new SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);
                await Task.Run(() => client.Connect());
                
                // Test if we can list directory
                var items = await Task.Run(() => client.ListDirectory("/"));
                
                await Task.Run(() => client.Disconnect());
                
                _logger.LogInformation("SFTP connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SFTP connection test failed: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> TransferFileAsync(ConnectionSettings connection, string sourcePath, string destinationPath)
        {
            try
            {
                _logger.LogInformation($"Starting SFTP file transfer from {sourcePath} to {destinationPath}");

                if (!File.Exists(sourcePath))
                {
                    _logger.LogError($"Source file does not exist: {sourcePath}");
                    return false;
                }

                using var client = new SftpClient(connection.Host, connection.Port, connection.Username, connection.Password);
                await Task.Run(() => client.Connect());
                
                // Get just the filename for the remote path
                var fileName = Path.GetFileName(sourcePath);
                var remotePath = destinationPath;
                
                // Make sure remotePath ends with a slash if it's a directory
                if (!remotePath.EndsWith("/"))
                {
                    // Check if it's a directory path (no file extension)
                    if (!remotePath.Contains("."))
                    {
                        remotePath += "/";
                    }
                }
                
                // If it's a directory path, add the filename
                if (remotePath.EndsWith("/"))
                {
                    remotePath += fileName;
                }
                
                _logger.LogInformation($"Uploading {sourcePath} to {remotePath}");
                
                using var fileStream = File.OpenRead(sourcePath);
                await Task.Run(() => client.UploadFile(fileStream, remotePath, true)); // true for overwrite
                
                await Task.Run(() => client.Disconnect());
                
                _logger.LogInformation("File transfer completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"SFTP file transfer failed: {ex.Message}");
                return false;
            }
        }
    }
}
