using System;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DataSyncer.WindowsService.Services
{
    public class NamedPipeServer
    {
        private readonly string _pipeName;
        private NamedPipeServerStream? _pipeServer;
        private CancellationTokenSource? _cts;

        // Event triggered when a message is received
        public event EventHandler<string>? MessageReceived;

        public NamedPipeServer(string pipeName)
        {
            _pipeName = pipeName;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => Listen(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _pipeServer?.Dispose();
        }

        private async Task Listen(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                using (_pipeServer = new NamedPipeServerStream(_pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.Asynchronous))
                {
                    await _pipeServer.WaitForConnectionAsync(token);

                    var buffer = new byte[1024];
                    int bytesRead = await _pipeServer.ReadAsync(buffer, 0, buffer.Length, token);
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    // Check if this is a command that requires a response
                    bool needsResponse = message.Contains("\"Command\":\"START_TRANSFER\"") || 
                                         message.Contains("\"Command\":\"TEST_CONNECTION\"");

                    // Trigger the message received event
                    MessageReceived?.Invoke(this, message);

                    // Send a response for commands that need it
                    if (needsResponse)
                    {
                        try
                        {
                            // Default success response
                            string responseText = "{\"Success\":true,\"Message\":\"Command received\"}";
                            byte[] responseBytes = Encoding.UTF8.GetBytes(responseText);
                            await _pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, token);
                            await _pipeServer.FlushAsync(token);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error sending pipe response: {ex.Message}");
                        }
                    }
                }
            }
        }

        // For synchronous command polling (legacy support)
        public string ReceiveMessage()
        {
            // This is a stub for compatibility; real usage should be event-driven
            return string.Empty;
        }
    }
}
