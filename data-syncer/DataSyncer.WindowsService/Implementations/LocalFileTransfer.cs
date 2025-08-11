using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Logging;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.Core.DTOs;

namespace DataSyncer.WindowsService.Implementations
{
    /// <summary>
    /// Implementation of IFileTransferService for local file transfers between directories
    /// </summary>
    public class LocalFileTransfer : IFileTransferService
    {
        private readonly ILogger<LocalFileTransfer> _logger;

        public LocalFileTransfer(ILogger<LocalFileTransfer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Transfers multiple files from source to destination directory
        /// </summary>
        public async Task<TransferResultDto> TransferFilesAsync(List<FileItem> files, ConnectionSettings connection, FilterSettings filters)
        {
            var result = new TransferResultDto 
            { 
                Success = true,
                SourcePath = connection.SourcePath ?? string.Empty,
                DestinationPath = connection.DestinationPath ?? string.Empty,
                Protocol = ProtocolType.LOCAL,
                Logs = new List<TransferLog>() 
            };
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                _logger.LogInformation($"Starting local file transfer of {files.Count} files");
                
                // Ensure destination directory exists
                string destDir = connection.DestinationPath ?? string.Empty;
                if (!Directory.Exists(destDir))
                {
                    try
                    {
                        Directory.CreateDirectory(destDir);
                        _logger.LogInformation($"Created destination directory: {destDir}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to create destination directory: {destDir}");
                        result.Success = false;
                        result.ErrorMessage = $"Failed to create destination directory: {ex.Message}";
                        return result;
                    }
                }

                // Process each file
                foreach (var file in files)
                {
                    try
                    {
                        var fileStopwatch = System.Diagnostics.Stopwatch.StartNew();
                        string destPath = Path.Combine(destDir, file.FileName);

                        // Get file size before copying
                        long fileSize = new FileInfo(file.FullPath).Length;
                        
                        _logger.LogInformation($"Copying {file.FullPath} to {destPath}");
                        
                        // Copy the file
                        await Task.Run(() => File.Copy(file.FullPath, destPath, true));
                        
                        fileStopwatch.Stop();
                        
                        // Log successful transfer
                        result.Logs.Add(new TransferLog
                        {
                            Timestamp = DateTime.Now,
                            FileName = file.FileName,
                            SourcePath = file.FullPath,
                            DestinationPath = destPath,
                            Status = "Success",
                            Message = "File copied successfully",
                            FileSize = fileSize,
                            Duration = fileStopwatch.Elapsed,
                            Protocol = "LOCAL"
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to copy file {file.FileName}");
                        
                        // Log failed transfer
                        result.Logs.Add(new TransferLog
                        {
                            Timestamp = DateTime.Now,
                            FileName = file.FileName,
                            SourcePath = file.FullPath,
                            DestinationPath = Path.Combine(destDir, file.FileName),
                            Status = "Failed",
                            Message = ex.Message,
                            ErrorMessage = ex.Message,
                            Protocol = "LOCAL",
                            Duration = TimeSpan.FromSeconds(0)
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during local file transfer");
                result.Success = false;
                result.ErrorMessage = $"Error during local file transfer: {ex.Message}";
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            
            return result;
        }

        /// <summary>
        /// Tests if local file transfer is possible by checking directory access
        /// </summary>
        public async Task<bool> TestConnectionAsync(ConnectionSettings connection)
        {
            try
            {
                _logger.LogInformation("Testing local file transfer capability");
                
                // Check if source path exists and is accessible
                string sourcePath = connection.SourcePath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(sourcePath))
                {
                    _logger.LogError("Source path is empty");
                    return false;
                }
                
                if (!Directory.Exists(sourcePath))
                {
                    _logger.LogError($"Source directory does not exist: {sourcePath}");
                    return false;
                }
                
                // Try to access source directory
                await Task.Run(() => Directory.GetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly).Length);
                
                // Check if destination path exists or can be created
                string destPath = connection.DestinationPath ?? string.Empty;
                if (string.IsNullOrWhiteSpace(destPath))
                {
                    _logger.LogError("Destination path is empty");
                    return false;
                }
                
                if (!Directory.Exists(destPath))
                {
                    try
                    {
                        // Create directory temporarily to test write access
                        await Task.Run(() => Directory.CreateDirectory(destPath));
                        
                        // Delete it if it was created just for testing
                        if (Directory.GetFiles(destPath).Length == 0 && 
                            Directory.GetDirectories(destPath).Length == 0)
                        {
                            Directory.Delete(destPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cannot create destination directory: {destPath}");
                        return false;
                    }
                }
                else
                {
                    // Try to create a temporary file to test write permissions
                    string testFile = Path.Combine(destPath, $"test_{Guid.NewGuid()}.tmp");
                    try
                    {
                        await Task.Run(() => File.WriteAllText(testFile, "test"));
                        File.Delete(testFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Cannot write to destination directory: {destPath}");
                        return false;
                    }
                }
                
                _logger.LogInformation("Local file transfer test successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local file transfer test failed");
                return false;
            }
        }

        /// <summary>
        /// Transfers a single file from source to destination path
        /// </summary>
        public async Task<bool> TransferFileAsync(ConnectionSettings connection, string sourcePath, string destinationPath)
        {
            try
            {
                _logger.LogInformation($"Starting local file copy from {sourcePath} to {destinationPath}");
                
                // Check if source file exists
                if (!File.Exists(sourcePath))
                {
                    _logger.LogError($"Source file does not exist: {sourcePath}");
                    return false;
                }
                
                // Handle destination path
                if (Directory.Exists(destinationPath) || 
                    destinationPath.EndsWith("\\") || 
                    destinationPath.EndsWith("/"))
                {
                    // If destination is a directory, append the filename
                    string fileName = Path.GetFileName(sourcePath);
                    destinationPath = Path.Combine(destinationPath, fileName);
                }
                
                // Make sure destination directory exists
                string? destDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    await Task.Run(() => Directory.CreateDirectory(destDir));
                }
                
                // Copy the file
                await Task.Run(() => File.Copy(sourcePath, destinationPath, true));
                
                _logger.LogInformation("File copied successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Local file transfer failed: {ex.Message}");
                return false;
            }
        }
    }
}
