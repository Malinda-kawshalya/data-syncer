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
        private int _connectionTimeoutMs = 5000; // 5 second timeout

        public NamedPipeClient(string pipeName = "DataSyncerPipe")
        {
            _pipeName = pipeName;
            _semaphore = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Sets the connection timeout in milliseconds
        /// </summary>
        public void SetConnectionTimeout(int timeoutMs)
        {
            if (timeoutMs > 0)
            {
                _connectionTimeoutMs = timeoutMs;
            }
        }

        /// <summary>
        /// Sends a message to the server and waits for a response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The response from the server, or null if no response received</returns>
        public async Task<string?> SendMessageAsync(string message)
        {
            Console.WriteLine($"Sending message: {message}");
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
                Console.WriteLine("Message sent successfully");

                // Read the response with a large buffer
                byte[] buffer = new byte[8192]; // 8KB buffer
                int bytesRead = await _pipeClient.ReadAsync(buffer);

                if (bytesRead > 0)
                {
                    var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received response: {response}");
                    return response;
                }

                Console.WriteLine("No response received");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
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
                DisposePipeClient();
                _pipeClient = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);

                try
                {
                    Console.WriteLine($"Connecting to pipe server (timeout: {_connectionTimeoutMs}ms)...");
                    await _pipeClient.ConnectAsync(_connectionTimeoutMs);
                    Console.WriteLine("Connected to pipe server successfully");
                }
                catch (TimeoutException)
                {
                    DisposePipeClient();
                    throw new InvalidOperationException($"Timeout while connecting to pipe server '{_pipeName}'. Ensure the service is running.");
                }
                catch (Exception ex)
                {
                    DisposePipeClient();
                    throw new InvalidOperationException($"Error connecting to pipe server: {ex.Message}", ex);
                }
            }
        }
        
        private void DisposePipeClient()
        {
            if (_pipeClient != null)
            {
                try
                {
                    _pipeClient.Dispose();
                    _pipeClient = null;
                }
                catch
                {
                    // Ignore disposal errors
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
                Console.WriteLine($"Sending command: {command}");
                var message = new
                {
                    Command = command,
                    Data = data,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(message);
                
                // Always wait for a response to ensure the command was processed
                var response = await SendMessageAsync(json);
                
                // Check if we got a response
                if (!string.IsNullOrEmpty(response))
                {
                    try
                    {
                        using var responseDoc = JsonDocument.Parse(response);
                        if (responseDoc.RootElement.TryGetProperty("Success", out var successElement))
                        {
                            bool success = successElement.GetBoolean();
                            
                            // Log response message if available
                            if (responseDoc.RootElement.TryGetProperty("Message", out var messageElement))
                            {
                                Console.WriteLine($"Response: {messageElement.GetString()}");
                            }
                            
                            return success;
                        }
                        // If no Success property, consider any response as success
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing response: {ex.Message}");
                        // If response parsing fails, consider any response as success
                        return true;
                    }
                }
                
                Console.WriteLine("No response received from server");
                return false; // No response means failure
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
            catch (Exception ex)
            {
                Console.WriteLine($"Connection test failed: {ex.Message}");
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
                    DisposePipeClient();
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
