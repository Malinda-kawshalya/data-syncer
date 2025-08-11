using DataSyncer.Core.Models;
using DataSyncer.Core.Services;
using DataSyncer.Service.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataSyncer.WindowsService
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly FileTransferWorker _fileTransferWorker;
        private readonly NamedPipeServer _pipeServer;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
            _fileTransferWorker = new FileTransferWorker();
            _pipeServer = new NamedPipeServer("DataSyncerPipe");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DataSyncer Service is running...");

            // Listen for commands from UI
            _ = Task.Run(() => ListenForCommands(stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }

        private void ListenForCommands(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var command = _pipeServer.ReceiveMessage();
                _logger.LogInformation($"Command received: {command}");

                if (command == "START")
                {
                    _fileTransferWorker.StartJobs();
                }
                else if (command == "STOP")
                {
                    _fileTransferWorker.StopJobs();
                }
            }
        }
    }
}
