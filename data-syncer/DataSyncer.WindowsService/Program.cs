using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using NLog.Extensions.Logging;
using Quartz;
using DataSyncer.Core.Interfaces;
using DataSyncer.WindowsService.Services;
using DataSyncer.WindowsService.Implementations;
using DataSyncer.WindowsService;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((ctx, cfg) => {
        // default loads appsettings.json
    })
    .ConfigureServices((context, services) =>
    {
        // Register all file transfer implementations
        services.AddSingleton<FluentFtpTransfer>(); 
        services.AddSingleton<LocalFileTransfer>();
        
        // Register the factory
        services.AddSingleton<FileTransferServiceFactory>();
        
        // Register the IFileTransferService as a factory
        services.AddSingleton<IFileTransferService>(provider => 
            provider.GetRequiredService<FileTransferServiceFactory>()
                .CreateFileTransferService(DataSyncer.Core.Models.ProtocolType.FTP));
        
        // Logging service
        services.AddSingleton<LoggingService>();

        // Named pipe server with explicit pipe name
        services.AddSingleton<NamedPipeServer>(provider => 
            new NamedPipeServer(
                provider.GetRequiredService<LoggingService>(), 
                "DataSyncerPipe"));

        // Quartz
        services.AddQuartz(q =>
        {
            var cron = context.Configuration.GetSection("ServiceSettings")?.GetValue<string>("DefaultScheduleCron") ?? "0 0/5 * * * ?";
            var jobKey = new JobKey("TransferJob");
            q.AddJob<TransferJob>(opts => opts.WithIdentity(jobKey));
            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("TransferJob-trigger")
                .WithCronSchedule(cron));
        });
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        // Hosted worker services
        services.AddHostedService<Worker>();

        // Logging with NLog
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.SetMinimumLevel(LogLevel.Information);
            loggingBuilder.AddNLog("nlog.config");
        });
    })
    // Allow run as a Windows Service
    .UseWindowsService()
    .Build();

await host.RunAsync();
