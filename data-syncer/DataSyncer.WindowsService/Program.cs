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
        // Core services
        services.AddSingleton<IFileTransferService, FluentFtpTransfer>(); 

        // Named pipe server with explicit pipe name
        services.AddSingleton<NamedPipeServer>(provider => 
            new NamedPipeServer("DataSyncerPipe"));

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
