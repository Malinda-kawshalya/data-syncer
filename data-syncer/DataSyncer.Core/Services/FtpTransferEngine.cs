using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentFTP;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.Services
{
    public class FtpTransferEngine : ITransferEngine
    {
        private readonly ConnectionSettings _connectionSettings;
        private readonly ILogger _logger;

        public FtpTransferEngine(ConnectionSettings connectionSettings, ILogger logger)
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
                using (var client = new FtpClient(_connectionSettings.Host, _connectionSettings.Port,
                                                _connectionSettings.Username, _connectionSettings.Password))
                {
                    client.Config.DataConnectionType = _connectionSettings.UsePassiveMode ?
                        FtpDataConnectionType.PASV : FtpDataConnectionType.PORT;
                    client.Config.ConnectTimeout = _connectionSettings.TimeoutSeconds * 1000;

                    await client.ConnectAsync();
                    _logger.LogInfo($"Connected to FTP server: {_connectionSettings.Host}");

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

                    await client.DisconnectAsync();
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
                result.Message = $"FTP transfer failed: {ex.Message}";
                _logger.LogError(result.Message, ex);
            }

            return result;
        }

        private async Task<FileTransferResult> TransferSingleFileAsync(FtpClient client, FileInfo file, TransferSettings settings)
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

                if (settings.Direction == TransferDirection.Upload)
                {
                    var uploadResult = await client.UploadFileAsync(file.FullName, remotePath,
                        settings.OverwriteExisting ? FtpRemoteExists.Overwrite : FtpRemoteExists.Skip);

                    result.Success = uploadResult == FtpStatus.Success;
                    result.Message = $"Upload {(result.Success ? "successful" : "failed")}";
                }
                else if (settings.Direction == TransferDirection.Download)
                {
                    var downloadResult = await client.DownloadFileAsync(file.FullName, remotePath,
                        settings.OverwriteExisting ? FtpLocalExists.Overwrite : FtpLocalExists.Skip);

                    result.Success = downloadResult == FtpStatus.Success;
                    result.Message = $"Download {(result.Success ? "successful" : "failed")}";
                }

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
                using (var client = new FtpClient(_connectionSettings.Host, _connectionSettings.Port,
                                                _connectionSettings.Username, _connectionSettings.Password))
                {
                    client.Config.ConnectTimeout = _connectionSettings.TimeoutSeconds * 1000;
                    await client.ConnectAsync();
                    await client.DisconnectAsync();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"FTP connection test failed: {ex.Message}", ex);
                return false;
            }
        }
    }
}