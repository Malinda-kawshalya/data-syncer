using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DataSyncer.Core.Models;
using DataSyncer.Core.Interfaces;
using DataSyncer.WindowsService.Services;
using DataSyncer.WindowsService.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DataSyncer.WindowsService.Services
{
    public class FileTransferWorker : IHostedService
    {
        private readonly ILogger<FileTransferWorker> _logger;
        private readonly NamedPipeServer _pipe;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly LoggingService _loggingService;
        private readonly FileTransferServiceFactory _transferFactory;
        private IScheduler? _scheduler;

        public FileTransferWorker(
            ILogger<FileTransferWorker> logger,
            NamedPipeServer pipe,
            LoggingService loggingService,
            FileTransferServiceFactory transferFactory,
            ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _pipe = pipe;
            _loggingService = loggingService;
            _transferFactory = transferFactory;
            _schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileTransferWorker starting");

            try
            {
                // Start Quartz scheduler
                _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
                await _scheduler.Start(cancellationToken);

                _logger.LogInformation("FileTransferWorker started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting FileTransferWorker");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileTransferWorker stopping");

            try
            {
                if (_scheduler != null)
                {
                    await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
                }

                _logger.LogInformation("FileTransferWorker stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping FileTransferWorker");
            }
        }

        public async Task TriggerTransferAsync(ConnectionSettings settings)
        {
            _logger.LogInformation($"Triggering file transfer for {settings.Host}");

            try
            {
                // Execute transfer using the simple executor with the factory
                await SimpleTransferExecutor.ExecuteTransferAsync(settings, _logger, _loggingService, _transferFactory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing transfer");
                throw;
            }
        }

        public async Task PauseAllJobsAsync()
        {
            if (_scheduler != null)
            {
                await _scheduler.PauseAll();
                _logger.LogInformation("All scheduled jobs paused");
            }
        }

        public async Task ResumeAllJobsAsync()
        {
            if (_scheduler != null)
            {
                await _scheduler.ResumeAll();
                _logger.LogInformation("All scheduled jobs resumed");
            }
        }
        
        /// <summary>
        /// Tests the connection using the appropriate transfer service based on protocol
        /// </summary>
        public async Task<bool> TestConnectionAsync(ConnectionSettings settings)
        {
            try
            {
                if (settings == null)
                {
                    _logger.LogError("Connection settings are null");
                    Console.WriteLine("=== Error: Connection settings are null ===");
                    return false;
                }
                
                _logger.LogInformation($"Testing connection to {settings.Host}:{settings.Port}");
                
                // Special case for local transfers
                if (IsLocalTransfer(settings))
                {
                    _logger.LogInformation("Local file transfer - validating paths");
                    Console.WriteLine("=== Local file transfer - validating paths ===");
                    
                    // Clean and normalize the paths
                    string sourcePath = settings.SourcePath?.Trim().Trim('"') ?? string.Empty;
                    string destPath = settings.DestinationPath?.Trim().Trim('"') ?? string.Empty;
                    
                    // Replace forward slashes with backslashes for Windows paths
                    sourcePath = sourcePath.Replace('/', '\\');
                    destPath = destPath.Replace('/', '\\');
                    
                    Console.WriteLine($"=== Source path: {sourcePath} ===");
                    Console.WriteLine($"=== Destination path: {destPath} ===");
                    
                    // For source, check if file exists or directory exists
                    bool sourceValid = false;
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        bool fileExists = File.Exists(sourcePath);
                        bool dirExists = Directory.Exists(sourcePath);
                        sourceValid = fileExists || dirExists;
                        
                        Console.WriteLine($"=== Source path: {sourcePath} ===");
                        Console.WriteLine($"=== File exists: {fileExists} ===");
                        Console.WriteLine($"=== Directory exists: {dirExists} ===");
                        Console.WriteLine($"=== Source path valid: {sourceValid} ===");
                        
                        // Check if file might exist with slightly different path
                        if (!sourceValid)
                        {
                            try
                            {
                                string directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                                string filename = Path.GetFileName(sourcePath);
                                if (Directory.Exists(directory))
                                {
                                    Console.WriteLine($"=== Directory exists: {directory} ===");
                                    var files = Directory.GetFiles(directory);
                                    Console.WriteLine($"=== Files in directory: {files.Length} ===");
                                    foreach (var file in files.Take(5)) // List up to 5 files to avoid flooding logs
                                    {
                                        Console.WriteLine($"=== Found file: {Path.GetFileName(file)} ===");
                                    }
                                }
                                else
                                {
                                    Console.WriteLine($"=== Directory does not exist: {directory} ===");
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"=== Error checking directory: {ex.Message} ===");
                            }
                        }
                    }
                    
                    // For destination, just check it's not empty
                    bool destValid = !string.IsNullOrEmpty(destPath);
                    Console.WriteLine($"=== Destination path valid: {destValid} ===");
                    
                    return sourceValid && destValid;
                }
                
                // For remote transfers
                // Create the appropriate transfer service based on protocol
                var transferService = GetTransferService(settings);
                if (transferService != null)
                {
                    return await transferService.TestConnectionAsync(settings);
                }
                
                _logger.LogWarning($"No transfer service found for protocol: {settings.Protocol}");
                Console.WriteLine($"=== No transfer service found for protocol: {settings.Protocol} ===");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error testing connection: {ex.Message}");
                Console.WriteLine($"=== Error testing connection: {ex.Message} ===");
                return false;
            }
        }
        
        private bool IsLocalTransfer(ConnectionSettings settings)
        {
            // Consider it a local transfer if:
            // 1. Both paths are local OR
            // 2. Protocol is FTP but host is localhost or 127.0.0.1
            bool bothPathsLocal = IsLocalPath(settings.SourcePath) && IsLocalPath(settings.DestinationPath);
            bool isLocalhost = settings.Host?.ToLower() == "localhost" || settings.Host == "127.0.0.1";
            
            return bothPathsLocal || isLocalhost;
        }
        
        /// <summary>
        /// Gets the appropriate transfer service based on connection protocol using the factory
        /// </summary>
        private IFileTransferService GetTransferService(ConnectionSettings settings)
        {
            return _transferFactory.CreateFileTransferService(settings.Protocol);
        }
        
        private bool IsLocalPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
                
            // Clean up the path a bit
            string cleanPath = path.Trim().Trim('"');
            
            // Check for Windows drive letter pattern (C:\) or UNC path (\\server\)
            return (cleanPath.Length >= 3 && 
                    char.IsLetter(cleanPath[0]) && 
                    cleanPath[1] == ':' && 
                    (cleanPath[2] == '\\' || cleanPath[2] == '/')) || 
                   cleanPath.StartsWith("\\\\");
        }
    }
}
