using System;
using System.Collections.Generic;

namespace DataSyncer.Core.Models
{
    public class TransferResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int FilesTransferred { get; set; }
        public int FilesSkipped { get; set; }
        public int FilesFailed { get; set; }
        public long TotalBytesTransferred { get; set; }
        public TimeSpan Duration { get; set; }
        public List<FileTransferResult> FileResults { get; set; } = new List<FileTransferResult>();
        public Exception Exception { get; set; }
    }

    public class FileTransferResult
    {
        public string FileName { get; set; } = "";
        public string LocalPath { get; set; } = "";
        public string RemotePath { get; set; } = "";
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long FileSize { get; set; }
        public DateTime TransferTime { get; set; }
        public Exception Exception { get; set; }
    }
}