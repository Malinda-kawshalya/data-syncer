using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DataSyncer.Core.Models;
using DataSyncer.Core.Utilities;

namespace DataSyncer.Core.Services
{
    public class TransferLogManager
    {
        private static readonly string LogsFolder;
        private static readonly string CurrentLogFile;
        private static readonly object _lockObj = new object();
        
        static TransferLogManager()
        {
            // Set up logging paths
            LogsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DataSyncer", "Logs");
                
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(LogsFolder))
            {
                Directory.CreateDirectory(LogsFolder);
            }
            
            // Current day's log file
            CurrentLogFile = Path.Combine(LogsFolder, $"transfer_log_{DateTime.Now:yyyyMMdd}.json");
        }
        
        /// <summary>
        /// Appends a transfer log to the current log file
        /// </summary>
        public static void LogTransfer(TransferLog log)
        {
            try
            {
                lock (_lockObj)
                {
                    // Get existing logs for today
                    List<TransferLog> logs;
                    
                    if (File.Exists(CurrentLogFile))
                    {
                        string json = File.ReadAllText(CurrentLogFile);
                        logs = JsonSerializer.Deserialize<List<TransferLog>>(json) ?? new List<TransferLog>();
                    }
                    else
                    {
                        logs = new List<TransferLog>();
                    }
                    
                    // Add the new log
                    logs.Add(log);
                    
                    // Save back to file
                    string updatedJson = JsonSerializer.Serialize(logs, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    
                    File.WriteAllText(CurrentLogFile, updatedJson);
                }
            }
            catch (Exception ex)
            {
                // Since this is the logger itself, just write to console
                Console.WriteLine($"Error logging transfer: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets logs for a specific date
        /// </summary>
        public static List<TransferLog> GetLogs(DateTime date)
        {
            try
            {
                string logFile = Path.Combine(LogsFolder, $"transfer_log_{date:yyyyMMdd}.json");
                
                if (File.Exists(logFile))
                {
                    string json = File.ReadAllText(logFile);
                    return JsonSerializer.Deserialize<List<TransferLog>>(json) ?? new List<TransferLog>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving logs: {ex.Message}");
            }
            
            return new List<TransferLog>();
        }
        
        /// <summary>
        /// Gets all available logs across all days
        /// </summary>
        public static List<TransferLog> GetAllLogs()
        {
            try
            {
                var allLogs = new List<TransferLog>();
                
                foreach (var file in Directory.GetFiles(LogsFolder, "transfer_log_*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var logs = JsonSerializer.Deserialize<List<TransferLog>>(json);
                        
                        if (logs != null)
                        {
                            allLogs.AddRange(logs);
                        }
                    }
                    catch
                    {
                        // Skip files with parsing errors
                        continue;
                    }
                }
                
                // Sort by timestamp descending
                allLogs.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
                
                return allLogs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving all logs: {ex.Message}");
                return new List<TransferLog>();
            }
        }
        
        /// <summary>
        /// Clears all logs in the local storage
        /// </summary>
        public static void ClearLogs()
        {
            try
            {
                lock (_lockObj)
                {
                    foreach (var file in Directory.GetFiles(LogsFolder, "transfer_log_*.json"))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error deleting log file {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing logs: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the path to the logs folder
        /// </summary>
        public static string GetLogsFolder() => LogsFolder;
    }
}
