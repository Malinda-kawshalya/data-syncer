using System.Collections.Generic;

namespace DataSyncer.Core.Models
{
    public class FilterSettings
    {
        public List<string> IncludeFileTypes { get; set; } = new();
        public long MinFileSizeBytes { get; set; }
        public long MaxFileSizeBytes { get; set; }
        
        // Added properties for more flexible filtering
        public bool IncludeSubfolders { get; set; }
        public int MinAgeMinutes { get; set; }
        public bool DeleteAfterTransfer { get; set; }
        public bool MoveToArchiveAfterTransfer { get; set; }
        public string ArchivePath { get; set; } = string.Empty;
    }
}
