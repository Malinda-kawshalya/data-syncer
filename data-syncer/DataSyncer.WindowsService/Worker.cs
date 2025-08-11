using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.WindowsService.Services;
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
        private ConnectionSettings? _lastConnectionSettings; // Store the most recent connection settings

        public Worker(
            ILogger<Worker> logger,
            NamedPipeServer pipeServer,
            ISchedulerFactory schedulerFactory,
            ILogger<FileTransferWorker> fileTransferLogger)
        {
            _logger = logger;
            _pipeServer = pipeServer;
            _fileTransferWorker = new FileTransferWorker(fileTransferLogger, pipeServer, schedulerFactory);
            
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

        private async void OnMessageReceived(object? sender, string message)
        {
            try
            {
                _logger.LogInformation($"Received message: {message}");
                Console.WriteLine($"=== Received message: {message} ===");

                // Try to parse as JSON command
                var command = JsonSerializer.Deserialize<JsonElement>(message);
                
                if (command.TryGetProperty("Command", out var cmdElement))
                {
                    var commandName = cmdElement.GetString();
                    _logger.LogInformation($"Processing command: {commandName}");
                    Console.WriteLine($"=== Processing command: {commandName} ===");

                    // Extract data if available
                    JsonElement? dataElement = null;
                    if (command.TryGetProperty("Data", out var data))
                    {
                        dataElement = data;
                    }

                    switch (commandName?.ToUpper())
                    {
                        case "PING":
                            _logger.LogInformation("Received PING command - Service is alive");
                            break;

                        case "TEST_CONNECTION":
                            await HandleTestConnection(dataElement);
                            break;

                        case "UPDATE_CONNECTION":
                            await HandleUpdateConnection(dataElement);
                            break;

                        case "START_TRANSFER":
                            await HandleStartTransfer(dataElement);
                            break;

                        case "STOP_SERVICE":
                            _logger.LogInformation("Received STOP_SERVICE command");
                            
                            break;

                        default:
                            _logger.LogWarning($"Unknown command: {commandName}");
                            break;
                    }
                }
                else
                {
                    _logger.LogWarning($"Invalid command format: {message}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message: {message}");
            }
        }

        private async Task HandleTestConnection(JsonElement? data)
        {
            try
            {
                // Get settings from command data if available
                ConnectionSettings? settings = null;
                
                if (data.HasValue && data.Value.ValueKind != JsonValueKind.Null)
                {
                    settings = JsonSerializer.Deserialize<ConnectionSettings>(data.Value.GetRawText());
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
                    }
                    else
                    {
                        _logger.LogWarning("Connection test failed");
                        Console.WriteLine("=== Connection test failed ===");
                    }
                }
                else
                {
                    Console.WriteLine("=== Error: No valid connection settings available ===");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                Console.WriteLine($"=== Error testing connection: {ex.Message} ===");
            }
            
            // No need to return anything in async Task method
        }

        private Task HandleUpdateConnection(JsonElement? data)
        {
            try
            {
                if (data.HasValue && data.Value.ValueKind != JsonValueKind.Null)
                {
                    var settings = JsonSerializer.Deserialize<ConnectionSettings>(data.Value.GetRawText());
                    if (settings != null)
                    {
                        // Store the settings for future use
                        _lastConnectionSettings = settings;
                        
                        _logger.LogInformation($"Updating connection settings for {settings.Host}");
                        Console.WriteLine($"=== Updated connection settings for {settings.Host}:{settings.Port} ===");
                        Console.WriteLine($"=== Source path: {settings.SourcePath} ===");
                        Console.WriteLine($"=== Destination path: {settings.DestinationPath} ===");
                        // Connection settings are already saved by the UI via ConfigurationManager
                    }
                }
                else
                {
                    Console.WriteLine("=== Error: No connection settings provided for update ===");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating connection settings");
                Console.WriteLine($"=== Error updating connection settings: {ex.Message} ===");
            }
            
            return Task.CompletedTask;
        }

        private async Task HandleStartTransfer(JsonElement? data)
        {
            try
            {
                // Get settings from command or use stored settings
                ConnectionSettings? settings = null;
                
                if (data.HasValue && data.Value.ValueKind != JsonValueKind.Null)
                {
                    settings = JsonSerializer.Deserialize<ConnectionSettings>(data.Value.GetRawText());
                }
                else if (_lastConnectionSettings != null)
                {
                    settings = _lastConnectionSettings;
                    Console.WriteLine("=== Using previously stored connection settings ===");
                }
                else
                {
                    Console.WriteLine("=== Error: No connection settings available ===");
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
                        Console.WriteLine($"=== ERROR: Source file not found: {settings.SourcePath} ===");
                        return;
                    }
                    
                    // Trigger a file transfer using the FileTransferWorker
                    await _fileTransferWorker.TriggerTransferAsync(settings);
                    
                    _logger.LogInformation("Transfer initiated successfully");
                    Console.WriteLine("=== Transfer initiated successfully ===");
                }
                else
                {
                    _logger.LogWarning("Cannot start transfer - invalid connection settings");
                    Console.WriteLine("=== Cannot start transfer - invalid connection settings ===");
                    Console.WriteLine($"=== Source: {settings.SourcePath} ===");
                    Console.WriteLine($"=== Destination: {settings.DestinationPath} ===");
                    Console.WriteLine($"=== Protocol: {settings.Protocol} ===");
                    Console.WriteLine($"=== Host: {settings.Host} ===");
                }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting transfer");
                Console.WriteLine($"=== Error starting transfer: {ex.Message} ===");
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
