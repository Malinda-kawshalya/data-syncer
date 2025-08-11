using System.Threading;
using System.Threading.Tasks;
using DataSyncer.WindowsService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

namespace DataSyncer.Service.Services
{
    public class FileTransferWorker : IHostedService
    {
        private readonly ILogger<FileTransferWorker> _logger;
        private readonly NamedPipeServer _pipe;
        private readonly ISchedulerFactory _schedulerFactory;
        private IScheduler _scheduler;

        public FileTransferWorker(
            ILogger<FileTransferWorker> logger,
            NamedPipeServer pipe,
            ISchedulerFactory schedulerFactory)
        {
            _logger = logger;
            _pipe = pipe;
            _schedulerFactory = schedulerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileTransferWorker starting");

            // Start Quartz scheduler
            _scheduler = await _schedulerFactory.GetScheduler(cancellationToken);
            await _scheduler.Start(cancellationToken);

            // Hook into Named Pipe server for commands
            _pipe.MessageReceived += async (sender, message) =>
            {
                _logger.LogInformation($"Received pipe command: {message}");

                if (message.Equals("START", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Starting scheduled jobs...");
                    await _scheduler.ResumeAll(cancellationToken);
                }
                else if (message.Equals("STOP", System.StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Pausing scheduled jobs...");
                    await _scheduler.PauseAll(cancellationToken);
                }
                else
                {
                    _logger.LogWarning($"Unknown command received: {message}");
                }
            };

            _pipe.Start();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("FileTransferWorker stopping");

            _pipe.Stop();

            if (_scheduler != null)
            {
                await _scheduler.Shutdown(waitForJobsToComplete: true, cancellationToken);
            }
        }
    }
}
