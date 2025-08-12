using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using DataSyncer.Core.DTOs;
using DataSyncer.Core.Models;
using DataSyncer.Core.Interfaces;
using Microsoft.Extensions.Logging;
using DataSyncer.WindowsService.Implementations;
using System.Collections.Generic;

namespace DataSyncer.WindowsService.Services
{
    /// <summary>
    /// Simple executor for file transfers
    /// </summary>
    internal static class SimpleTransferExecutor
    {
        /// <summary>
        /// Executes a file transfer based on the provided connection settings
        /// </summary>
        public static async Task ExecuteTransferAsync(ConnectionSettings settings, ILogger logger, LoggingService loggingService, FileTransferServiceFactory transferFactory)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                logger.LogInformation("Starting file transfer for Host: {host}, Protocol: {protocol}", 
                    settings.Host ?? "LOCAL", settings.Protocol);
                
                // Determine protocol type (default to LOCAL for local paths)
                ProtocolType protocol = settings.Protocol;
                if (protocol != ProtocolType.LOCAL && IsLocalPath(settings.SourcePath) && IsLocalPath(settings.DestinationPath))
                {
                    protocol = ProtocolType.LOCAL;
                    logger.LogDebug("Auto-detected local file transfer based on paths");
                }
                
                // Get the appropriate transfer service
                var transferService = transferFactory.CreateFileTransferService(protocol);
                
                // Prepare file list
                var files = new List<FileItem>();
                
                // Source path handling
                string sourcePath = settings.SourcePath?.Trim('\"') ?? string.Empty;
                
                if (Directory.Exists(sourcePath))
                {
                    // Get all files in the directory
                    foreach (var filePath in Directory.GetFiles(sourcePath))
                    {
                        var fileInfo = new FileInfo(filePath);
                        files.Add(new FileItem
                        {
                            FileName = fileInfo.Name,
                            FullPath = filePath,
                            SizeBytes = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                    }
                    
                    logger.LogInformation("Found {fileCount} files in directory: {sourcePath}", files.Count, sourcePath);
                }
                else if (File.Exists(sourcePath))
                {
                    // Single file transfer
                    var fileInfo = new FileInfo(sourcePath);
                    files.Add(new FileItem
                    {
                        FileName = fileInfo.Name,
                        FullPath = sourcePath,
                        SizeBytes = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                    
                    logger.LogInformation("Transferring single file: {fileName} ({size} bytes)", fileInfo.Name, fileInfo.Length);
                }
                else
                {
                    throw new FileNotFoundException($"Source path not found: {sourcePath}");
                }
                
                if (files.Count == 0)
                {
                    logger.LogWarning("No files found to transfer at: {sourcePath}", sourcePath);
                    
                    // Log the empty transfer
                    loggingService.LogTransfer(new TransferResultDto
                    {
                        SourcePath = sourcePath,
                        DestinationPath = settings.DestinationPath,
                        Protocol = protocol,
                        Success = true,
                        ErrorMessage = "No files found to transfer",
                        Duration = stopwatch.Elapsed,
                        FileSize = 0
                    });
                    
                    return;
                }
                
                // Execute the transfer using the selected service
                var result = await transferService.TransferFilesAsync(files, settings, new FilterSettings());
                
                // Log the transfer result
                loggingService.LogTransfer(result);
                
                logger.LogInformation("Transfer completed - Status: {status}, Files: {count}, Duration: {duration}ms", 
                    result.Success ? "Success" : "Failed", files.Count, stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transfer failed: {message}", ex.Message);
                
                // Log the error
                loggingService.LogTransfer(new TransferResultDto
                {
                    SourcePath = settings.SourcePath,
                    DestinationPath = settings.DestinationPath,
                    Protocol = settings.Protocol,
                    Success = false,
                    ErrorMessage = $"Transfer failed: {ex.Message}",
                    Duration = stopwatch.Elapsed,
                    FileSize = 0
                });
                
                throw; // Re-throw to let caller handle the exception
            }
        }
        
        /// <summary>
        /// Checks if a path is a local path
        /// </summary>
        private static bool IsLocalPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            string cleanPath = path.Trim().Trim('"');
            
            // Check for Windows drive letter pattern (C:\) or UNC path (\\server\)
            return (cleanPath.Length >= 3 && 
                    char.IsLetter(cleanPath[0]) && 
                    cleanPath[1] == ':' && 
                    (cleanPath[2] == '\\' || cleanPath[2] == '/')) || 
                   cleanPath.StartsWith("\\\\") ||
                   Path.IsPathRooted(cleanPath);
        }
    }
}
