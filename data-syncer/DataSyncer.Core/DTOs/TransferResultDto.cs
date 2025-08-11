using System.Collections.Generic;
using DataSyncer.Core.Models;

namespace DataSyncer.Core.DTOs
{
    public class TransferResultDto
    {
        public bool Success { get; set; }
        public List<TransferLog> Logs { get; set; } = new();
    }
}
