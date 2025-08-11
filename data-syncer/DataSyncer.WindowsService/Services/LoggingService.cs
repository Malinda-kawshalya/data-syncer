using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DataSyncer.Core.DTOs;
using DataSyncer.Core.Models;

namespace DataSyncer.WindowsService.Services
{
    public class LoggingService
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DataSyncer",
            "Logs");
            
        private static readonly string LogFilePath = Path.Combine(LogDirectory, "transfer_logs.json");
        
        // Queue to hold logs that need to be written to disk
        private readonly Queue<TransferLog> _pendingLogs = new();
        private bool _isWriting = false;
        private readonly object _lockObject = new();
        
        public LoggingService()
        {
            // Ensure log directory exists
            Directory.CreateDirectory(LogDirectory);
            
            // Create the log file if it doesn't exist
            if (!File.Exists(LogFilePath))
            {
                File.WriteAllText(LogFilePath, "[]");
            }
        }
        
        /// <summary>
        /// Logs a file transfer
        /// </summary>
        /// <param name="result">Result of the transfer</param>
        public void LogTransfer(TransferResultDto result)
        {
            var log = new TransferLog
            {
                Timestamp = DateTime.Now,
                SourcePath = result.SourcePath,
                DestinationPath = result.DestinationPath,
                Status = result.Success ? "Success" : "Failed",
                FileSize = result.FileSize,
                Duration = result.Duration,
                ErrorMessage = result.ErrorMessage,
                Protocol = result.Protocol.ToString()
            };
            
            // Add to pending logs queue
            lock (_lockObject)
            {
                _pendingLogs.Enqueue(log);
            }
            
            // Trigger write process if not already running
            _ = WritePendingLogsAsync();
        }
        
        /// <summary>
        /// Retrieves all transfer logs
        /// </summary>
        /// <returns>List of transfer logs</returns>
        public List<TransferLog> GetAllLogs()
        {
            try
            {
                if (!File.Exists(LogFilePath))
                {
                    return new List<TransferLog>();
                }
                
                string json = File.ReadAllText(LogFilePath);
                return JsonSerializer.Deserialize<List<TransferLog>>(json) ?? new List<TransferLog>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading logs: {ex.Message}");
                return new List<TransferLog>();
            }
        }
        
        /// <summary>
        /// Retrieves transfer logs that match the given filter
        /// </summary>
        /// <param name="filter">Optional filter string to match against log fields</param>
        /// <param name="maxResults">Maximum number of results to return</param>
        /// <returns>Filtered list of transfer logs</returns>
        public List<TransferLog> GetLogs(string? filter = null, int maxResults = 100)
        {
            var allLogs = GetAllLogs();
            
            // Apply filtering if filter is provided
            if (!string.IsNullOrWhiteSpace(filter))
            {
                filter = filter.ToLowerInvariant();
                allLogs = allLogs.FindAll(log =>
                    log.SourcePath.ToLowerInvariant().Contains(filter) ||
                    log.DestinationPath.ToLowerInvariant().Contains(filter) ||
                    log.Status.ToLowerInvariant().Contains(filter) ||
                    log.Protocol.ToLowerInvariant().Contains(filter) ||
                    (log.ErrorMessage?.ToLowerInvariant().Contains(filter) ?? false)
                );
            }
            
            // Sort by timestamp descending (newest first)
            allLogs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            
            // Limit results
            if (allLogs.Count > maxResults)
            {
                allLogs = allLogs.GetRange(0, maxResults);
            }
            
            return allLogs;
        }
        
        /// <summary>
        /// Clears all logs
        /// </summary>
        public void ClearLogs()
        {
            try
            {
                File.WriteAllText(LogFilePath, "[]");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing logs: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Writes pending logs to the log file
        /// </summary>
        private async Task WritePendingLogsAsync()
        {
            // Prevent multiple concurrent writes
            lock (_lockObject)
            {
                if (_isWriting)
                {
                    return;
                }
                _isWriting = true;
            }
            
            try
            {
                await Task.Run(() =>
                {
                    // Keep processing as long as there are pending logs
                    while (true)
                    {
                        TransferLog? log = null;
                        
                        // Dequeue a log while holding the lock
                        lock (_lockObject)
                        {
                            if (_pendingLogs.Count > 0)
                            {
                                log = _pendingLogs.Dequeue();
                            }
                            else
                            {
                                // No more logs to process
                                _isWriting = false;
                                return;
                            }
                        }
                        
                        // Process the log outside the lock
                        if (log != null)
                        {
                            try
                            {
                                // Read existing logs
                                var logs = GetAllLogs();
                                
                                // Add new log
                                logs.Add(log);
                                
                                // Write back to file
                                string json = JsonSerializer.Serialize(logs, new JsonSerializerOptions { WriteIndented = true });
                                File.WriteAllText(LogFilePath, json);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error writing log: {ex.Message}");
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in log writing task: {ex.Message}");
                
                // Reset writing flag if an exception occurs
                lock (_lockObject)
                {
                    _isWriting = false;
                }
            }
        }
    }
}
