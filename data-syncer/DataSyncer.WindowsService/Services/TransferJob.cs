using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Quartz;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.DTOs;
using DataSyncer.Core.Models; // Explicitly using DataSyncer.Core.Models.ProtocolType
using System.Collections.Generic;

namespace DataSyncer.WindowsService.Services
{
    [DisallowConcurrentExecution]
    public class TransferJob : IJob
    {
        private readonly IFileTransferService _transferService;
        private readonly ILogger<TransferJob> _logger;

        public TransferJob(IFileTransferService transferService, ILogger<TransferJob> logger)
        {
            _transferService = transferService;
            _logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation("TransferJob started at {time}", System.DateTimeOffset.Now);

            var files = new List<FileItem>();
            // TODO: enumerate files from configured folder, apply filters
            var connection = new ConnectionSettings
            {
                Host = "example.com",
                Port = 21,
                Username = "user",
                Password = "pass",
                Protocol = DataSyncer.Core.Models.ProtocolType.FTP // Fully qualify ProtocolType
            };
            var filters = new FilterSettings();

            var result = await _transferService.TransferFilesAsync(files, connection, filters);

            foreach (var log in result.Logs)
            {
                _logger.LogInformation("{time} | {file} | {status} | {msg}", log.Timestamp, log.FileName, log.Status, log.Message);
            }

            _logger.LogInformation("TransferJob finished at {time}", System.DateTimeOffset.Now);
        }
    }
}
