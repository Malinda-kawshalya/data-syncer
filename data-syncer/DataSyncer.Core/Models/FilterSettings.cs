using System.Collections.Generic;

namespace DataSyncer.Core.Models
{
    public class FilterSettings
    {
        public List<string> IncludeFileTypes { get; set; } = new();
        public long MinFileSizeBytes { get; set; }
        public long MaxFileSizeBytes { get; set; }
    }
}
