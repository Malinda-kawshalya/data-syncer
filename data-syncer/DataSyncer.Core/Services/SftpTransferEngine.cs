using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Renci.SshNet;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.Services
{
    public class SftpTransferEngine : ITransferEngine
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger _logger;

        public SftpTransferEngine(ConnectionSettings connectionSettings, ILogger logger)
        {
            _connectionSettings = connectionSettings ?? throw new ArgumentNullException(nameof(connectionSettings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TransferResult> TransferFilesAsync(TransferSettings transferSettings, List<FileInfo> files)
        {
            var result = new TransferResult();
            var startTime = DateTime.Now;

            try
            {
                var connectionInfo = new ConnectionInfo(_connectionSettings.Host, _connectionSettings.Port,
                    _connectionSettings.Username, new PasswordAuthenticationMethod(_connectionSettings.Username, _connectionSettings.Password));

                connectionInfo.Timeout = TimeSpan.FromSeconds(_connectionSettings.TimeoutSeconds);

                using (var client = new SftpClient(connectionInfo))
                {
                    await Task.Run(() => client.Connect());
                    _logger.LogInfo($"Connected to SFTP server: {_connectionSettings.Host}");

                    foreach (var file in files)
                    {
                        var fileResult = await TransferSingleFileAsync(client, file, transferSettings);
                        result.FileResults.Add(fileResult);

                        if (fileResult.Success)
                        {
                            result.FilesTransferred++;
                            result.TotalBytesTransferred += fileResult.FileSize;
                        }
                        else
                        {
                            result.FilesFailed++;
                        }
                    }

                    client.Disconnect();
                }

                result.Success = result.FilesFailed == 0;
                result.Duration = DateTime.Now - startTime;
                result.Message = $"Transfer completed. Files: {result.FilesTransferred} transferred, " +
                               $"{result.FilesFailed} failed, {result.FilesSkipped} skipped";

                _logger.LogInfo(result.Message);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                result.Message = $"SFTP transfer failed: {ex.Message}";
                _logger.LogError(result.Message, ex);
            }

            return result;
        }

        private async Task<FileTransferResult> TransferSingleFileAsync(SftpClient client, FileInfo file, TransferSettings settings)
        {
            var result = new FileTransferResult
            {
                FileName = file.Name,
                LocalPath = file.FullName,
                FileSize = file.Length,
                TransferTime = DateTime.Now
            };

            try
            {
                string remotePath = Path.Combine(settings.RemotePath, file.Name).Replace('\\', '/');
                result.RemotePath = remotePath;

                await Task.Run(() =>
                {
                    if (settings.Direction == TransferDirection.Upload)
                    {
                        using (var fileStream = file.OpenRead())
                        {
                            client.UploadFile(fileStream, remotePath, settings.OverwriteExisting);
                        }
                        result.Success = true;
                        result.Message = "Upload successful";
                    }
                    else if (settings.Direction == TransferDirection.Download)
                    {
                        using (var fileStream = File.Create(file.FullName))
                        {
                            client.DownloadFile(remotePath, fileStream);
                        }
                        result.Success = true;
                        result.Message = "Download successful";
                    }
                });

                if (result.Success)
                {
                    _logger.LogInfo($"File {settings.Direction.ToString().ToLower()}: {file.Name}");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Exception = ex;
                result.Message = $"Transfer failed: {ex.Message}";
                _logger.LogError($"File transfer error for {file.Name}: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var connectionInfo = new ConnectionInfo(_connectionSettings.Host, _connectionSettings.Port,
                    _connectionSettings.Username, new PasswordAuthenticationMethod(_connectionSettings.Username, _connectionSettings.Password));

                connectionInfo.Timeout = TimeSpan.FromSeconds(_connectionSettings.TimeoutSeconds);

                using (var client = new SftpClient(connectionInfo))
                {
                    await Task.Run(() =>
                    {
                        client.Connect();
                        client.Disconnect();
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SFTP connection test failed: {ex.Message}", ex);
                return false;
            }
        }
    }
}