using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Quartz;
using DataSyncer.Core.Interfaces;
using DataSyncer.Service.Services;
using DataSyncer.Service.Implementations;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) => {
        // default loads appsettings.json
    })
    .ConfigureServices((context, services) =>
    {
        // Core services
        services.AddSingleton<IFileTransferService, FluentFtpTransfer>(); // default, can switch on job

        // Named pipe server
        services.AddSingleton<NamedPipeServer>();

        // Quartz
        services.AddQuartz(q =>
        {
            q.UseMicrosoftDependencyInjectionJobFactory();
            // A simple example: schedule TransferJob with a cron from config
            var cron = context.Configuration.GetSection("ServiceSettings")?.GetValue<string>("DefaultScheduleCron") ?? "0 0/5 * * * ?";
            var jobKey = new JobKey("TransferJob");
            q.AddJob<TransferJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("TransferJob-trigger")
                .WithCronSchedule(cron));
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Hosted worker to start named pipe and orchestrate
        services.AddHostedService<FileTransferWorker>();

        // Logging with NLog
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
            loggingBuilder.AddNLog("nlog.config");
        });

        // Add configuration binding etc as needed
    })
    // Allow run as a Windows Service
    .UseWindowsService()
    .Build();

await host.RunAsync();
