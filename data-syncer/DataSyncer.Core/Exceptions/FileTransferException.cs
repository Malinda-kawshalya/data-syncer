using System;

namespace DataSyncer.Core.Exceptions
{
    public class FileTransferException : Exception
    {
        public FileTransferException() { }
        public FileTransferException(string message) : base(message) { }
        public FileTransferException(string message, Exception inner) : base(message, inner) { }
    }
}
