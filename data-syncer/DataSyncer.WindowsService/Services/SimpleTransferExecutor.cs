using System;
using System.IO;
using System.Threading.Tasks;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using DataSyncer.WindowsService.Implementations;
using Microsoft.Extensions.Logging;

namespace DataSyncer.WindowsService.Services
{
    public class SimpleTransferExecutor
    {
        public static async Task ExecuteTransferAsync(ConnectionSettings settings, ILogger logger)
        {
            logger.LogInformation($"Starting transfer from {settings.SourcePath} to {settings.DestinationPath}");
            Console.WriteLine($"=== Starting transfer from {settings.SourcePath} to {settings.DestinationPath} ===");
            Console.WriteLine($"=== Target: {settings.Protocol}://{settings.Host}:{settings.Port} ===");

            try
            {
                // Check if we're doing a local file transfer
                if (IsLocalPath(settings.SourcePath) && IsLocalPath(settings.DestinationPath))
                {
                    Console.WriteLine("=== Performing local file transfer ===");
                    await PerformLocalTransfer(settings, logger);
                    return;
                }

                // Otherwise, use the appropriate transfer service
                IFileTransferService transferService = GetTransferService(settings, logger);
                
                // First test the connection
                Console.WriteLine("=== Testing connection... ===");
                bool connectionSuccess = await transferService.TestConnectionAsync(settings);
                
                if (!connectionSuccess)
                {
                    logger.LogError("Connection test failed - canceling transfer");
                    Console.WriteLine("=== Connection test failed - canceling transfer ===");
                    return;
                }

                Console.WriteLine("=== Connection successful - starting file transfer ===");
                bool transferSuccess = await transferService.TransferFileAsync(settings, settings.SourcePath, settings.DestinationPath);

                if (transferSuccess)
                {
                    logger.LogInformation("Transfer completed successfully");
                    Console.WriteLine("=== Transfer completed successfully ===");
                }
                else
                {
                    logger.LogError("Transfer failed");
                    Console.WriteLine("=== Transfer failed ===");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transfer failed");
                Console.WriteLine($"=== Transfer failed: {ex.Message} ===");
                throw;
            }
        }
        
        private static bool IsLocalPath(string path)
        {
            return !string.IsNullOrEmpty(path) && 
                   (path.Contains(":\\") || path.StartsWith("\\\\"));
        }
        
        private static Task PerformLocalTransfer(ConnectionSettings settings, ILogger logger)
        {
            try
            {
                // Remove any quotes that might be in the path (common when copying from UI)
                string sourcePath = settings.SourcePath?.Trim('"') ?? string.Empty;
                string destPath = settings.DestinationPath?.Trim('"') ?? string.Empty;
                
                Console.WriteLine($"=== Local transfer from {sourcePath} to {destPath} ===");
                
                // Check if source exists
                if (!File.Exists(sourcePath))
                {
                    logger.LogError($"Source file does not exist: {sourcePath}");
                    Console.WriteLine($"=== Source file does not exist: {sourcePath} ===");
                    return Task.CompletedTask;
                }
                
                // Ensure destination directory exists
                string? destDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
                
                // If destination is a directory, append the filename
                if (Directory.Exists(destPath))
                {
                    destPath = Path.Combine(destPath, Path.GetFileName(sourcePath));
                }
                
                // Perform the copy
                Console.WriteLine($"=== Copying {sourcePath} to {destPath} ===");
                File.Copy(sourcePath, destPath, true);
                
                logger.LogInformation($"File copied from {sourcePath} to {destPath}");
                Console.WriteLine($"=== File successfully copied to: {destPath} ===");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Local file transfer failed");
                Console.WriteLine($"=== Local file transfer failed: {ex.Message} ===");
                return Task.CompletedTask;
            }
        }
        
        private static IFileTransferService GetTransferService(ConnectionSettings settings, ILogger logger)
        {
            // Create the appropriate logger type with a cast - not ideal but works for demo
            return settings.Protocol switch
            {
                ProtocolType.FTP => new FluentFtpTransfer((ILogger<FluentFtpTransfer>)(object)logger),
                ProtocolType.SFTP => new SshNetTransfer((ILogger<SshNetTransfer>)(object)logger),
                _ => new DataSyncer.Core.Services.FileTransferService()
            };
        }
    }
}
