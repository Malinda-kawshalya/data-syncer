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
            _logger.LogInformation("Triggering file transfer for Host: {host}, Protocol: {protocol}", 
                settings.Host ?? "LOCAL", settings.Protocol);

            try
            {
                // Execute transfer using the simple executor with the factory
                await SimpleTransferExecutor.ExecuteTransferAsync(settings, _logger, _loggingService, _transferFactory);
                _logger.LogInformation("Transfer completed successfully");
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
                    return false;
                }
                
                _logger.LogInformation($"Testing connection to {settings.Host}:{settings.Port}");
                
                // Special case for local transfers
                if (IsLocalTransfer(settings))
                {
                    _logger.LogInformation("Local file transfer - validating paths");
                    
                    // Clean and normalize the paths
                    string sourcePath = settings.SourcePath?.Trim().Trim('"') ?? string.Empty;
                    string destPath = settings.DestinationPath?.Trim().Trim('"') ?? string.Empty;
                    
                    // Replace forward slashes with backslashes for Windows paths
                    sourcePath = sourcePath.Replace('/', '\\');
                    destPath = destPath.Replace('/', '\\');
                    
                    _logger.LogDebug("Source path: {sourcePath}, Destination path: {destPath}", sourcePath, destPath);
                    
                    // For source, check if file exists or directory exists
                    bool sourceValid = false;
                    if (!string.IsNullOrEmpty(sourcePath))
                    {
                        bool fileExists = File.Exists(sourcePath);
                        bool dirExists = Directory.Exists(sourcePath);
                        sourceValid = fileExists || dirExists;
                        
                        _logger.LogDebug("Source validation - File exists: {fileExists}, Directory exists: {dirExists}, Valid: {sourceValid}", 
                            fileExists, dirExists, sourceValid);
                        
                        // If source not found, provide diagnostic information
                        if (!sourceValid)
                        {
                            try
                            {
                                string directory = Path.GetDirectoryName(sourcePath) ?? string.Empty;
                                if (Directory.Exists(directory))
                                {
                                    var files = Directory.GetFiles(directory);
                                    _logger.LogDebug("Directory exists but file not found. Directory: {directory}, Files count: {count}", 
                                        directory, files.Length);
                                    
                                    // Log first few file names for debugging
                                    var sampleFiles = files.Take(3).Select(Path.GetFileName);
                                    _logger.LogDebug("Sample files in directory: {files}", string.Join(", ", sampleFiles));
                                }
                                else
                                {
                                    _logger.LogWarning("Directory does not exist: {directory}", directory);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error checking directory: {message}", ex.Message);
                            }
                        }
                    }
                    
                    // For destination, validate path format and parent directory
                    bool destValid = !string.IsNullOrEmpty(destPath) && ValidateDestinationPath(destPath);
                    _logger.LogDebug("Destination path valid: {destValid}", destValid);
                    
                    return sourceValid && destValid;
                }
                
                // For remote transfers
                var transferService = GetTransferService(settings);
                if (transferService != null)
                {
                    return await transferService.TestConnectionAsync(settings);
                }
                
                _logger.LogWarning("No transfer service found for protocol: {protocol}", settings.Protocol);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection: {message}", ex.Message);
                return false;
            }
        }
        
        private bool IsLocalTransfer(ConnectionSettings settings)
        {
            // Check if protocol is explicitly set to LOCAL
            if (settings.Protocol == ProtocolType.LOCAL)
                return true;
                
            // Consider it a local transfer if:
            // 1. Both paths are local file paths OR
            // 2. Host is localhost/127.0.0.1 (for backward compatibility)
            bool bothPathsLocal = IsLocalPath(settings.SourcePath) && IsLocalPath(settings.DestinationPath);
            bool isLocalhost = settings.Host?.ToLower() == "localhost" || settings.Host == "127.0.0.1";
            
            return bothPathsLocal || isLocalhost;
        }
        
        
        /// Gets the appropriate transfer service based on connection protocol using the factory
       
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

      
        /// Validates if the destination path format is correct and parent directory exists
        
        private bool ValidateDestinationPath(string destPath)
        {
            if (string.IsNullOrEmpty(destPath))
                return false;

            try
            {
                // Check if it's a valid path format
                string? parentDir = Path.GetDirectoryName(destPath);
                
                // If destination is a directory path, check if it exists or can be created
                if (destPath.EndsWith("\\") || destPath.EndsWith("/"))
                {
                    return Directory.Exists(destPath) || Directory.Exists(parentDir ?? string.Empty);
                }
                
                // If destination is a file path, check if parent directory exists
                return !string.IsNullOrEmpty(parentDir) && Directory.Exists(parentDir);
            }
            catch
            {
                return false;
            }
        }
    }
}
