using System;
using System.Collections.Generic;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.DTOs
{
    /// <summary>
    /// Data transfer object for transfer results
    /// </summary>
    public class TransferResultDto
    {
        /// <summary>
        /// Whether the transfer was successful
        /// </summary>
        public bool Success { get; set; }
        
        /// <summary>
        /// Collection of transfer logs
        /// </summary>
        public List<TransferLog> Logs { get; set; } = new();
        
        /// <summary>
        /// Source file or directory path
        /// </summary>
        public string SourcePath { get; set; } = string.Empty;
        
        /// <summary>
        /// Destination file or directory path
        /// </summary>
        public string DestinationPath { get; set; } = string.Empty;
        
        /// <summary>
        /// Error message if the transfer failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Protocol used for the transfer
        /// </summary>
        public ProtocolType Protocol { get; set; }
        
        /// <summary>
        /// Size of the transferred file in bytes
        /// </summary>
        public long FileSize { get; set; }
        
        /// <summary>
        /// Duration of the transfer
        /// </summary>
        public TimeSpan Duration { get; set; }
    }
}
