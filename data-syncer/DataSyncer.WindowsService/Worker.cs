using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.WindowsService.Services;
using DataSyncer.WindowsService.Implementations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Quartz;

namespace DataSyncer.WindowsService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly FileTransferWorker _fileTransferWorker;
        private readonly NamedPipeServer _pipeServer;
        private readonly LoggingService _loggingService;
        private ConnectionSettings? _lastConnectionSettings; // Store the most recent connection settings

        public Worker(
            ILogger<Worker> logger,
            NamedPipeServer pipeServer,
            ISchedulerFactory schedulerFactory,
            LoggingService loggingService,
            FileTransferServiceFactory transferFactory,
            ILogger<FileTransferWorker> fileTransferLogger)
        {
            _logger = logger;
            _pipeServer = pipeServer;
            _loggingService = loggingService;
            _fileTransferWorker = new FileTransferWorker(
                fileTransferLogger, 
                pipeServer, 
                loggingService,
                transferFactory, 
                schedulerFactory);
            
            // Subscribe to pipe messages
            _pipeServer.MessageReceived += OnMessageReceived;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataSyncer Service is starting...");
            Console.WriteLine("=== DataSyncer Service is starting... ===");

            try
            {
                // Start the named pipe server
                _pipeServer.Start();
                _logger.LogInformation("Named pipe server started");
                Console.WriteLine("=== Named pipe server started on pipe: DataSyncerPipe ===");

                // Start the file transfer worker
                await _fileTransferWorker.StartAsync(stoppingToken);
                _logger.LogInformation("File transfer worker started");
                Console.WriteLine("=== File transfer worker started ===");

                _logger.LogInformation("DataSyncer Service is running and ready for commands");
                Console.WriteLine("=== DataSyncer Service is running and ready for commands ===");

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in service execution");
                throw;
            }
        }

        private async void OnMessageReceived(object? sender, PipeMessageEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Received command: {e.Command}");
                Console.WriteLine($"=== Received command: {e.Command} ===");

                switch (e.Command.ToUpper())
                {
                    case "PING":
                        _logger.LogInformation("Received PING command - Service is alive");
                        _pipeServer.SetCommandResult(true, "Pong");
                        break;

                    case "TEST_CONNECTION":
                        await HandleTestConnection(e.Data);
                        break;

                    case "UPDATE_CONNECTION":
                        await HandleUpdateConnection(e.Data);
                        break;

                    case "START_TRANSFER":
                        await HandleStartTransfer(e.Data);
                        break;

                    case "STOP_SERVICE":
                        _logger.LogInformation("Received STOP_SERVICE command");
                        _pipeServer.SetCommandResult(true, "Service stopping...");
                        break;

                    default:
                        _logger.LogWarning($"Unknown command: {e.Command}");
                        _pipeServer.SetCommandResult(false, $"Unknown command: {e.Command}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing command: {e.Command}");
                _pipeServer.SetCommandResult(false, $"Error processing command: {ex.Message}");
            }
        }

        private async Task HandleTestConnection(object? data)
        {
            try
            {
                // Get settings from command data if available
                ConnectionSettings? settings = null;
                
                if (data != null)
                {
                    // Try to convert from JsonElement if that's what we got
                    if (data is JsonElement jsonElement && jsonElement.ValueKind != JsonValueKind.Null)
                    {
                        settings = JsonSerializer.Deserialize<ConnectionSettings>(jsonElement.GetRawText());
                    }
                    // If data is already a ConnectionSettings object (rare but possible)
                    else if (data is ConnectionSettings connectionSettings)
                    {
                        settings = connectionSettings;
                    }
                }
                
                // If no settings provided in the command, use the stored settings
                if (settings == null)
                {
                    settings = _lastConnectionSettings;
                    Console.WriteLine("=== Using previously stored connection settings ===");
                }
                
                // Validate settings before testing
                if (settings != null && !string.IsNullOrEmpty(settings.Host))
                {
                    _logger.LogInformation($"Testing connection to {settings.Host}:{settings.Port}");
                    Console.WriteLine($"=== Testing connection to {settings.Host}:{settings.Port} ===");
                    
                    // Perform actual connection test using the appropriate service
                    bool success = await _fileTransferWorker.TestConnectionAsync(settings);
                    
                    if (success)
                    {
                        _logger.LogInformation("Connection test successful");
                        Console.WriteLine("=== Connection test successful ===");
                        _pipeServer.SetCommandResult(true, "Connection test successful");
                    }
                    else
                    {
                        _logger.LogWarning("Connection test failed");
                        Console.WriteLine("=== Connection test failed ===");
                        _pipeServer.SetCommandResult(false, "Connection test failed");
                    }
                }
                else
                {
                    Console.WriteLine("=== Error: No valid connection settings available ===");
                    _pipeServer.SetCommandResult(false, "No valid connection settings available");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                Console.WriteLine($"=== Error testing connection: {ex.Message} ===");
                _pipeServer.SetCommandResult(false, $"Error testing connection: {ex.Message}");
            }
        }

        private Task HandleUpdateConnection(object? data)
        {
            try
            {
                ConnectionSettings? settings = null;
                
                // Try to convert from JsonElement if that's what we got
                if (data is JsonElement jsonElement && jsonElement.ValueKind != JsonValueKind.Null)
                {
                    settings = JsonSerializer.Deserialize<ConnectionSettings>(jsonElement.GetRawText());
                }
                // If data is already a ConnectionSettings object (rare but possible)
                else if (data is ConnectionSettings connectionSettings)
                {
                    settings = connectionSettings;
                }
                
                if (settings != null)
                {
                    // Store the settings for future use
                    _lastConnectionSettings = settings;
                    
                    _logger.LogInformation($"Updating connection settings for {settings.Host}");
                    Console.WriteLine($"=== Updated connection settings for {settings.Host}:{settings.Port} ===");
                    Console.WriteLine($"=== Source path: {settings.SourcePath} ===");
                    Console.WriteLine($"=== Destination path: {settings.DestinationPath} ===");
                    
                    // Set success result
                    _pipeServer.SetCommandResult(true, "Connection settings updated successfully");
                }
                else
                {
                    Console.WriteLine("=== Error: No connection settings provided for update ===");
                    _pipeServer.SetCommandResult(false, "No connection settings provided for update");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection settings");
                Console.WriteLine($"=== Error updating connection settings: {ex.Message} ===");
                _pipeServer.SetCommandResult(false, $"Error updating connection settings: {ex.Message}");
            }
            
            return Task.CompletedTask;
        }

        private async Task HandleStartTransfer(object? data)
        {
            try
            {
                // Get settings from command or use stored settings
                ConnectionSettings? settings = null;
                
                // Try to convert from JsonElement if that's what we got
                if (data is JsonElement jsonElement && jsonElement.ValueKind != JsonValueKind.Null)
                {
                    settings = JsonSerializer.Deserialize<ConnectionSettings>(jsonElement.GetRawText());
                }
                // If data is already a ConnectionSettings object (rare but possible)
                else if (data is ConnectionSettings connectionSettings)
                {
                    settings = connectionSettings;
                }
                else if (_lastConnectionSettings != null)
                {
                    settings = _lastConnectionSettings;
                    Console.WriteLine("=== Using previously stored connection settings ===");
                }
                else
                {
                    Console.WriteLine("=== Error: No connection settings available ===");
                    _pipeServer.SetCommandResult(false, "No connection settings available");
                    return;
                }
                
                // Clean up paths by removing any quotes
                if (settings != null)
                {
                    if (settings.SourcePath != null) settings.SourcePath = settings.SourcePath.Trim('"');
                    if (settings.DestinationPath != null) settings.DestinationPath = settings.DestinationPath.Trim('"');
                
                    if (settings.IsValid())
                    {
                        _logger.LogInformation($"Starting transfer from {settings.SourcePath} to {settings.DestinationPath}");
                        Console.WriteLine($"=== Starting transfer from {settings.SourcePath} to {settings.DestinationPath} ===");
                        
                        // Check if source file exists
                        bool sourceExists = !string.IsNullOrEmpty(settings.SourcePath) && 
                                           System.IO.File.Exists(settings.SourcePath);
                                           
                        if (!sourceExists)
                        {
                            string errorMsg = $"Source file not found: {settings.SourcePath}";
                            Console.WriteLine($"=== ERROR: {errorMsg} ===");
                            _pipeServer.SetCommandResult(false, errorMsg);
                            return;
                        }
                        
                        // Trigger a file transfer using the FileTransferWorker
                        await _fileTransferWorker.TriggerTransferAsync(settings);
                        
                        _logger.LogInformation("Transfer initiated successfully");
                        Console.WriteLine("=== Transfer initiated successfully ===");
                        _pipeServer.SetCommandResult(true, "Transfer initiated successfully");
                    }
                    else
                    {
                        string errorMsg = "Cannot start transfer - invalid connection settings";
                        _logger.LogWarning(errorMsg);
                        Console.WriteLine($"=== {errorMsg} ===");
                        Console.WriteLine($"=== Source: {settings.SourcePath} ===");
                        Console.WriteLine($"=== Destination: {settings.DestinationPath} ===");
                        Console.WriteLine($"=== Protocol: {settings.Protocol} ===");
                        Console.WriteLine($"=== Host: {settings.Host} ===");
                        
                        _pipeServer.SetCommandResult(false, errorMsg);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transfer");
                Console.WriteLine($"=== Error starting transfer: {ex.Message} ===");
                _pipeServer.SetCommandResult(false, $"Error starting transfer: {ex.Message}");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DataSyncer Service is stopping...");
            
            _pipeServer.Stop();
            await _fileTransferWorker.StopAsync(cancellationToken);
            
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("DataSyncer Service stopped");
        }
    }
}
