using System;

namespace DataSyncer.Core.Models
{
    public class TransferLog
    {
        public DateTime Timestamp { get; set; }
        public string FileName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
