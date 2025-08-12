using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.DTOs;
using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.WindowsService.Implementations;

namespace DataSyncer.WindowsService.Services
{
    [DisallowConcurrentExecution]
    public class TransferJob : IJob
    {
        private readonly ILogger<TransferJob> _logger;
        private readonly FileTransferServiceFactory _transferFactory;

        public TransferJob(ILogger<TransferJob> logger, FileTransferServiceFactory transferFactory)
        {
            _logger = logger;
            _transferFactory = transferFactory;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                _logger.LogInformation("Scheduled TransferJob started at {time}", DateTimeOffset.Now);
                Console.WriteLine($"=== Scheduled TransferJob started at {DateTimeOffset.Now} ===");
                
                // Get job data
                var dataMap = context.MergedJobDataMap;
                
                // First try to get connection settings from job data
                ConnectionSettings? connection = null;
                if (dataMap.ContainsKey("ConnectionSettings"))
                {
                    connection = (ConnectionSettings)dataMap["ConnectionSettings"];
                }
                
                // If not found in job data, try loading from config
                if (connection == null)
                {
                    connection = DataSyncer.Core.Services.ConfigurationManager.LoadConnectionSettings();
                }
                
                // If still null, can't continue
                if (connection == null || !connection.IsValid())
                {
                    _logger.LogError("Invalid or missing connection settings");
                    Console.WriteLine("=== Invalid or missing connection settings ===");
                    return;
                }
                
                // Get filter settings
                FilterSettings? filters = null;
                if (dataMap.ContainsKey("FilterSettings"))
                {
                    filters = (FilterSettings)dataMap["FilterSettings"];
                }
                else
                {
                    filters = DataSyncer.Core.Services.ConfigurationManager.LoadFilterSettings() ?? new FilterSettings();
                }
                
                // Ensure source path exists
                if (string.IsNullOrEmpty(connection.SourcePath))
                {
                    _logger.LogError("Source path is not configured");
                    Console.WriteLine("=== Source path is not configured ===");
                    return;
                }
                
                string sourcePath = connection.SourcePath.Trim('"');
                
                // Handle different source paths
                List<FileItem> files = new List<FileItem>();
                if (File.Exists(sourcePath))
                {
                    // Single file
                    var fileInfo = new FileInfo(sourcePath);
                    files.Add(new FileItem
                    {
                        FullPath = fileInfo.FullName,
                        FileName = fileInfo.Name,
                        SizeBytes = fileInfo.Length,
                        LastModified = fileInfo.LastWriteTime
                    });
                    
                    _logger.LogInformation($"Added single file for transfer: {fileInfo.Name}");
                }
                else if (Directory.Exists(sourcePath))
                {
                    // Directory - get files based on filters
                    SearchOption searchOption = filters.IncludeSubfolders ? 
                                               SearchOption.AllDirectories : 
                                               SearchOption.TopDirectoryOnly;
                    
                    // Get all files that match any of the file types
                    var allFiles = Directory.GetFiles(sourcePath, "*.*", searchOption);
                    
                    foreach (var filePath in allFiles)
                    {
                        var fileInfo = new FileInfo(filePath);
                        
                        // Apply file type filter if specified
                        if (filters.IncludeFileTypes.Count > 0)
                        {
                            string extension = fileInfo.Extension.ToLowerInvariant();
                            if (!filters.IncludeFileTypes.Contains(extension))
                            {
                                continue;
                            }
                        }
                        
                        // Apply file size filter
                        if (fileInfo.Length < filters.MinFileSizeBytes || 
                            (filters.MaxFileSizeBytes > 0 && fileInfo.Length > filters.MaxFileSizeBytes))
                        {
                            continue;
                        }
                        
                        // Apply date filter
                        if (filters.MinAgeMinutes > 0)
                        {
                            TimeSpan age = DateTime.Now - fileInfo.LastWriteTime;
                            if (age.TotalMinutes < filters.MinAgeMinutes)
                            {
                                continue;
                            }
                        }
                        
                        files.Add(new FileItem
                        {
                            FullPath = fileInfo.FullName,
                            FileName = fileInfo.Name,
                            SizeBytes = fileInfo.Length,
                            LastModified = fileInfo.LastWriteTime
                        });
                    }
                    
                    _logger.LogInformation($"Found {files.Count} files matching filters in {sourcePath}");
                    Console.WriteLine($"=== Found {files.Count} files matching filters in {sourcePath} ===");
                }
                else
                {
                    _logger.LogError($"Source path not found: {sourcePath}");
                    Console.WriteLine($"=== Source path not found: {sourcePath} ===");
                    return;
                }
                
                if (files.Count == 0)
                {
                    _logger.LogInformation("No files match transfer criteria");
                    Console.WriteLine("=== No files match transfer criteria ===");
                    return;
                }
                
                // Create the appropriate transfer service using the factory
                var transferService = _transferFactory.CreateFileTransferService(connection.Protocol);
                
                // Execute the transfer
                var result = await transferService.TransferFilesAsync(files, connection, filters);
                
                // Log results
                foreach (var log in result.Logs)
                {
                    if (log.Status?.Equals("Success", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogInformation("{time} | {file} | {status} | {msg}", 
                            log.Timestamp, log.FileName, log.Status, log.Message);
                    }
                    else
                    {
                        _logger.LogError("{time} | {file} | {status} | {msg}", 
                            log.Timestamp, log.FileName, log.Status, log.Message);
                    }
                    
                    // Save to persistent log
                    TransferLogManager.LogTransfer(log);
                }
                
                _logger.LogInformation("Scheduled TransferJob finished at {time}", DateTimeOffset.Now);
                Console.WriteLine($"=== Scheduled TransferJob finished at {DateTimeOffset.Now} ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing scheduled transfer job");
                Console.WriteLine($"=== Error executing scheduled transfer job: {ex.Message} ===");
                
                // Log the error to the transfer logs
                TransferLogManager.LogTransfer(new TransferLog
                {
                    Timestamp = DateTime.Now,
                    FileName = "ScheduledJob",
                    Status = "Failed",
                    Message = $"Error executing job: {ex.Message}"
                });
            }
        }
    }
}
