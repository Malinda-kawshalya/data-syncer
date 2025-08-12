using System;
using System.Collections.Generic;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.DTOs
{

    public class TransferResultDto
    {

        public bool Success { get; set; }

        public List<TransferLog> Logs { get; set; } = new();

        public string SourcePath { get; set; } = string.Empty;

        public string DestinationPath { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public ProtocolType Protocol { get; set; }

        public long FileSize { get; set; }
        

        public TimeSpan Duration { get; set; }
    }
}
