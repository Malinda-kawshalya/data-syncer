using System;
using DataSyncer.Core.Interfaces;
using DataSyncer.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DataSyncer.WindowsService.Implementations
{
    /// <summary>
    /// Factory for creating the appropriate file transfer service based on the protocol type
    /// </summary>
    public class FileTransferServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<FileTransferServiceFactory> _logger;

        public FileTransferServiceFactory(IServiceProvider serviceProvider, ILogger<FileTransferServiceFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Creates the appropriate file transfer service based on the protocol type
        /// </summary>
        /// <param name="protocol">The protocol type to use</param>
        /// <returns>The appropriate file transfer service</returns>
        public IFileTransferService CreateFileTransferService(ProtocolType protocol)
        {
            _logger.LogInformation($"Creating file transfer service for protocol: {protocol}");
            
            switch (protocol)
            {
                case ProtocolType.FTP:
                    _logger.LogInformation("Using FluentFtpTransfer service");
                    return _serviceProvider.GetRequiredService<FluentFtpTransfer>();
                    
                case ProtocolType.SFTP:
                    // If you have a separate SFTP implementation
                    _logger.LogInformation("Using SFTP service");
                    // TODO: Replace with actual SFTP service when implemented
                    return _serviceProvider.GetRequiredService<FluentFtpTransfer>();
                    
                case ProtocolType.LOCAL:
                    _logger.LogInformation("Using LocalFileTransfer service");
                    return _serviceProvider.GetRequiredService<LocalFileTransfer>();
                    
                default:
                    _logger.LogWarning($"Unknown protocol {protocol}, falling back to local transfer");
                    return _serviceProvider.GetRequiredService<LocalFileTransfer>();
            }
        }
    }
}
