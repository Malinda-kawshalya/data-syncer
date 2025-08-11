using System.ComponentModel.DataAnnotations;

namespace DataSyncer.Core.Models
{
    public enum ProtocolType
    {
        FTP,
        SFTP,
        LOCAL
    }

    public class ConnectionSettings
    {
        [Required]
        public string Host { get; set; } = string.Empty;
        
        [Range(1, 65535)]
        public int Port { get; set; } = 21;
        
        [Required]
        public string Username { get; set; } = string.Empty;
        
        [Required]
        public string Password { get; set; } = string.Empty;
        
        public ProtocolType Protocol { get; set; } = ProtocolType.FTP;
        
        public string SourcePath { get; set; } = string.Empty;
        public string DestinationPath { get; set; } = string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(Host) &&
                   !string.IsNullOrWhiteSpace(Username) &&
                   !string.IsNullOrWhiteSpace(Password) &&
                   Port > 0 && Port <= 65535;
        }
    }
}
