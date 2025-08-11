using System.IO;
using System.IO.Pipes;

namespace DataSyncer.WindowsService.Services
{
    public class NamedPipeServer
    {
        private readonly string _pipeName;

        public NamedPipeServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public Func<object, object, Task> MessageReceived { get; internal set; }

        public string ReceiveMessage()
        {
            using (var server = new NamedPipeServerStream(_pipeName, PipeDirection.In))
            {
                server.WaitForConnection();
                using (var reader = new StreamReader(server))
                {
                    return reader.ReadLine();
                }
            }
        }
    }
}
