using System;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.Services
{
    public static class TransferEngineFactory
    {
        public static ITransferEngine CreateEngine(ConnectionSettings connectionSettings, ILogger logger)
        {
            return connectionSettings.Protocol.ToUpper() switch
            {
                "FTP" => new FtpTransferEngine(connectionSettings, logger),
                "SFTP" => new SftpTransferEngine(connectionSettings, logger),
                _ => throw new ArgumentException($"Unsupported protocol: {connectionSettings.Protocol}")
            };
        }
    }
}