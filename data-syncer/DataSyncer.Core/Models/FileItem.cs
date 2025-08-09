using System;

namespace DataSyncer.Core.Models
{
    public class FileItem
    {
        public string FileName { get; set; }
        public long SizeBytes { get; set; }
        public DateTime LastModified { get; set; }
        public string FullPath { get; set; }
    }
}
