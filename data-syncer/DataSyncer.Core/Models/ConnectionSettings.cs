namespace DataSyncer.Core.Models
{
    public enum ProtocolType
    {
        FTP,
        SFTP
    }

    public class ConnectionSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 21;
        public string Username { get; set; }
        public string Password { get; set; }
        public ProtocolType Protocol { get; set; }
    }
}
