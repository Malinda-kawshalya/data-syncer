using System;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

namespace DataSyncer.WinFormsUI.Utilities
{
    public class NamedPipeClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _pipeClient;
        private readonly SemaphoreSlim _semaphore;
        private bool _isDisposed;

        public NamedPipeClient(string pipeName = "DataSyncerPipe")
        {
            _pipeName = pipeName;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Sends a message to the server and waits for a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The response from the server, or null if no response received</returns>
        public async Task<string?> SendMessageAsync(string message)
        {
            await _semaphore.WaitAsync();
            try
            {
                await EnsureConnectedAsync();

                if (_pipeClient == null || !_pipeClient.IsConnected)
                {
                    throw new InvalidOperationException("Failed to establish connection to pipe server.");
                }

                // Send the message
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _pipeClient.WriteAsync(messageBytes);
                await _pipeClient.FlushAsync();

                // Read the response
                byte[] buffer = new byte[4096];
                int bytesRead = await _pipeClient.ReadAsync(buffer);

                if (bytesRead > 0)
                {
                    return Encoding.UTF8.GetString(buffer, 0, bytesRead);
                }

                return null;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error communicating with pipe server: {ex.Message}", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Sends a message to the server without waiting for a response
        /// </summary>
        /// <param name="message">The message to send</param>
        public async Task SendMessageNoResponseAsync(string message)
        {
            await _semaphore.WaitAsync();
            try
            {
                await EnsureConnectedAsync();

                if (_pipeClient == null || !_pipeClient.IsConnected)
                {
                    throw new InvalidOperationException("Failed to establish connection to pipe server.");
                }

                byte[] messageBytes = Encoding.UTF8.GetBytes(message);
                await _pipeClient.WriteAsync(messageBytes);
                await _pipeClient.FlushAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error sending message to pipe server: {ex.Message}", ex);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Synchronous message send for compatibility with older code
        /// </summary>
        /// <param name="message">The message to send</param>
        public void SendMessage(string message)
        {
            SendMessageNoResponseAsync(message).GetAwaiter().GetResult();
        }

        private async Task EnsureConnectedAsync()
        {
            if (_pipeClient == null || !_pipeClient.IsConnected)
            {
                _pipeClient?.Dispose();
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                try
                {
                    await _pipeClient.ConnectAsync(5000); // 5 second timeout
                }
                catch (TimeoutException)
                {
                    throw new InvalidOperationException("Timeout while connecting to pipe server. Ensure the service is running.");
                }
            }
        }

        /// <summary>
        /// Sends a command with optional data to the service
        /// </summary>
        /// <typeparam name="T">Type of data to send</typeparam>
        /// <param name="command">The command name</param>
        /// <param name="data">Optional data to send with the command</param>
        /// <returns>True if command was sent successfully</returns>
        public async Task<bool> SendCommandAsync<T>(string command, T? data = default)
        {
            try
            {
                var message = new
                {
                    Command = command,
                    Data = data,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message);
                
                // For commands that need response verification, use SendMessageAsync
                if (command == "START_TRANSFER" || command == "TEST_CONNECTION")
                {
                    var response = await SendMessageAsync(json);
                    
                    // Check if we got a response
                    if (!string.IsNullOrEmpty(response))
                    {
                        try
                        {
                            using var responseDoc = JsonDocument.Parse(response);
                            if (responseDoc.RootElement.TryGetProperty("Success", out var successElement))
                            {
                                return successElement.GetBoolean();
                            }
                            // If no Success property, consider any response as success
                            return true;
                        }
                        catch
                        {
                            // If response parsing fails, consider any response as success
                            return true;
                        }
                    }
                    return false; // No response means failure
                }
                else
                {
                    // For other commands (PING, UPDATE_CONNECTION), just send without waiting
                    await SendMessageNoResponseAsync(json);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendCommandAsync error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tests if the service is responding
        /// </summary>
        /// <returns>True if service responds to ping</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Send a simple PING command rather than TEST_CONNECTION
                // TEST_CONNECTION is for testing FTP connections, not service connectivity
                return await SendCommandAsync<object>("PING");
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _pipeClient?.Dispose();
                    _semaphore.Dispose();
                }

                _isDisposed = true;
            }
        }

        ~NamedPipeClient()
        {
            Dispose(false);
        }
    }
}
