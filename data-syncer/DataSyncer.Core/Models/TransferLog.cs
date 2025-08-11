using System;

namespace DataSyncer.Core.Models
{
    public class TransferLog
    {
        public DateTime Timestamp { get; set; }
        public string? FileName { get; set; }
        public string? SourcePath { get; set; }
        public string? DestinationPath { get; set; }
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Protocol { get; set; }
        public long FileSize { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
