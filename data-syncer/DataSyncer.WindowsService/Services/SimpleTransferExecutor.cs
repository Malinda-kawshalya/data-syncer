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
                logger.LogInformation($"Starting file transfer for connection {settings.Host}");
                Console.WriteLine($"=== Starting file transfer for {settings.Host} ===");
                
                // Determine protocol type (default to LOCAL for local paths)
                ProtocolType protocol = settings.Protocol;
                if (IsLocalPath(settings.SourcePath) && IsLocalPath(settings.DestinationPath))
                {
                    protocol = ProtocolType.LOCAL;
                    Console.WriteLine("=== Detected local file transfer ===");
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
                        files.Add(new FileItem
                        {
                            FileName = Path.GetFileName(filePath),
                            FullPath = filePath
                        });
                    }
                    
                    logger.LogInformation($"Found {files.Count} files in {sourcePath}");
                    Console.WriteLine($"=== Found {files.Count} files in {sourcePath} ===");
                }
                else if (File.Exists(sourcePath))
                {
                    // Single file transfer
                    files.Add(new FileItem
                    {
                        FileName = Path.GetFileName(sourcePath),
                        FullPath = sourcePath
                    });
                    
                    logger.LogInformation($"Transferring single file: {sourcePath}");
                    Console.WriteLine($"=== Transferring single file: {sourcePath} ===");
                }
                else
                {
                    throw new FileNotFoundException($"Source path not found: {sourcePath}");
                }
                
                if (files.Count == 0)
                {
                    logger.LogWarning($"No files found to transfer at {sourcePath}");
                    Console.WriteLine($"=== No files found to transfer at {sourcePath} ===");
                    
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
                
                logger.LogInformation($"Transfer completed with status: {(result.Success ? "Success" : "Failed")}");
                Console.WriteLine($"=== Transfer completed with status: {(result.Success ? "Success" : "Failed")} ===");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transfer failed with exception");
                Console.WriteLine($"=== Transfer failed: {ex.Message} ===");
                
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
            }
        }
        
        /// <summary>
        /// Checks if a path is a local path
        /// </summary>
        private static bool IsLocalPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            return Path.IsPathRooted(path) || path.StartsWith("./") || path.StartsWith("../");
        }
    }
}
