using System.IO;
using System.IO.Pipes;

namespace DataSyncer.WinFormsUI.Utilities
{
    public class NamedPipeClient
    {
        private readonly string _pipeName;

        public NamedPipeClient(string pipeName = "DataSyncerPipe")
        {
            _pipeName = pipeName;
        }

        public void SendMessage(string message)
        {
            using (var client = new NamedPipeClientStream(".", _pipeName, PipeDirection.Out))
            {
                client.Connect(2000); // Wait up to 2 sec for service to connect
                using (var writer = new StreamWriter(client))
                {
                    writer.AutoFlush = true;
                    writer.WriteLine(message);
                }
            }
        }
    }
}
