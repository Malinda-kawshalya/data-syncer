using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using DataSyncer.Core.Models;

namespace DataSyncer.WindowsService.Services
{
    /// <summary>
    /// Named pipe server for IPC between the Windows service and the UI
    /// </summary>
    public class NamedPipeServer
    {
        private readonly string _pipeName;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cts;
        private readonly LoggingService _loggingService;

        // Event triggered when a message is received
        public event EventHandler<PipeMessageEventArgs>? MessageReceived;
        
        // Last command result for response to client
        private bool _lastCommandResult = true;
        private string _lastCommandMessage = "Command completed successfully";
        private object? _lastCommandData = null;

        public NamedPipeServer(LoggingService loggingService, string pipeName = "DataSyncerPipe")
        {
            _pipeName = pipeName;
            _loggingService = loggingService;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Listen(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _pipeServer?.Dispose();
        }
        
        /// <summary>
        /// Sets the result of the current command for response to client
        /// </summary>
        /// <param name="success">Whether the command was successful</param>
        /// <param name="message">Message to include in the response</param>
        /// <param name="data">Optional data to include in the response</param>
        public void SetCommandResult(bool success, string message, object? data = null)
        {
            _lastCommandResult = success;
            _lastCommandMessage = message;
            _lastCommandData = data;
        }

        private async Task Listen(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    using (_pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, 
                                PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                    {
                        Console.WriteLine($"=== Named pipe server waiting for connection on '{_pipeName}' ===");
                        await _pipeServer.WaitForConnectionAsync(token);
                        Console.WriteLine("=== Client connected to named pipe ===");

                        // Read the client message with a large buffer
                        var buffer = new byte[8192]; // 8KB buffer for larger commands
                        int bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, token);
                        
                        if (bytesRead > 0)
                        {
                            string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                            Console.WriteLine($"=== Received message from client: {message.Substring(0, Math.Min(message.Length, 200))}... ===");

                            // Extract command info for processing
                            string commandName = "UNKNOWN";
                            object? commandData = null;
                            
                            try
                            {
                                using var jsonDoc = JsonDocument.Parse(message);
                                var root = jsonDoc.RootElement;
                                
                                if (root.TryGetProperty("Command", out var cmdElement))
                                {
                                    commandName = cmdElement.GetString() ?? "UNKNOWN";
                                }
                                
                                if (root.TryGetProperty("Data", out var dataElement) && 
                                    dataElement.ValueKind != JsonValueKind.Null)
                                {
                                    // Keep the raw JSON element for later processing
                                    commandData = dataElement;
                                }
                            }
                            catch (Exception ex)
                            { 
                                Console.WriteLine($"Error parsing command JSON: {ex.Message}");
                            }

                            // Reset command result for new command
                            _lastCommandResult = true;
                            _lastCommandMessage = $"Command {commandName} received";
                            _lastCommandData = null;
                            
                            // Process special commands directly here
                            if (commandName == "PING")
                            {
                                // Simple ping command - just respond with success
                                _lastCommandResult = true;
                                _lastCommandMessage = "Pong";
                            }
                            else if (commandName == "GET_LOGS")
                            {
                                // Get transfer logs
                                try
                                {
                                    string? filter = null;
                                    int maxResults = 100;
                                    
                                    // Parse filter parameters if provided
                                    if (commandData != null && commandData is JsonElement dataElement)
                                    {
                                        if (dataElement.TryGetProperty("Filter", out var filterElement))
                                        {
                                            filter = filterElement.GetString();
                                        }
                                        
                                        if (dataElement.TryGetProperty("MaxResults", out var maxElement))
                                        {
                                            maxResults = maxElement.GetInt32();
                                        }
                                    }
                                    
                                    var logs = _loggingService.GetLogs(filter, maxResults);
                                    _lastCommandResult = true;
                                    _lastCommandMessage = $"Retrieved {logs.Count} logs";
                                    _lastCommandData = logs;
                                }
                                catch (Exception ex)
                                {
                                    _lastCommandResult = false;
                                    _lastCommandMessage = $"Error retrieving logs: {ex.Message}";
                                }
                            }
                            else if (commandName == "CLEAR_LOGS")
                            {
                                // Clear all logs
                                try
                                {
                                    _loggingService.ClearLogs();
                                    _lastCommandResult = true;
                                    _lastCommandMessage = "All logs cleared successfully";
                                }
                                catch (Exception ex)
                                {
                                    _lastCommandResult = false;
                                    _lastCommandMessage = $"Error clearing logs: {ex.Message}";
                                }
                            }
                            else
                            {
                                // Trigger the message received event for other commands
                                MessageReceived?.Invoke(this, new PipeMessageEventArgs(commandName, commandData, message));
                            }

                            // Send response
                            try
                            {
                                // Build the response
                                var response = new
                                {
                                    Success = _lastCommandResult,
                                    Message = _lastCommandMessage,
                                    Data = _lastCommandData,
                                    Timestamp = DateTime.UtcNow
                                };
                                
                                var options = new JsonSerializerOptions
                                {
                                    WriteIndented = false, // Compact JSON
                                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                                };
                                
                                string responseText = JsonSerializer.Serialize(response, options);
                                byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);
                                
                                await _pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                                await _pipeServer.FlushAsync(token);
                                
                                Console.WriteLine($"=== Sent response: {responseText.Substring(0, Math.Min(responseText.Length, 200))}... ===");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error sending pipe response: {ex.Message}");
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Normal cancellation, just exit
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== Error in named pipe server: {ex.Message} ===");
                    // Brief delay before retrying to avoid tight loop
                    await Task.Delay(1000, CancellationToken.None);
                }
            }
            
            Console.WriteLine("=== Named pipe server stopped ===");
        }
    }
    
    /// <summary>
    /// Event args for pipe message received events
    /// </summary>
    public class PipeMessageEventArgs : EventArgs
    {
        public string Command { get; }
        public object? Data { get; }
        public string RawMessage { get; }
        
        public PipeMessageEventArgs(string command, object? data, string rawMessage)
        {
            Command = command;
            Data = data;
            RawMessage = rawMessage;
        }
    }
}
