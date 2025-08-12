using DataSyncer.Core.Models;
using System;
using System.Threading.Tasks;

namespace DataSyncer.WinFormsUI.Utilities
{
    public class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> _instance = new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Instance => _instance.Value;

        private readonly NamedPipeClient _pipeClient;

        // Current connection settings stored here
        public ConnectionSettings? CurrentSettings { get; private set; }
        
        private ConnectionManager()
        {
            _pipeClient = new NamedPipeClient();
        }

        // Update settings when user saves them
        public void UpdateSettings(ConnectionSettings settings)
        {
            CurrentSettings = settings;
            
            // Notify the service of the updated connection settings
            _ = SendConnectionUpdateAsync(settings);
        }
        

        public async Task<bool> SendConnectionUpdateAsync(ConnectionSettings settings)
        {
            return await _pipeClient.SendCommandAsync("UPDATE_CONNECTION", settings);
        }
        
    
        /// Tests the connection to the Windows service
       
        public async Task<bool> TestServiceConnectionAsync()
        {
            return await _pipeClient.TestConnectionAsync();
        }
        

        /// Tests the connection to the Windows service

        public async Task<bool> IsServiceConnectedAsync()
        {
            return await _pipeClient.TestConnectionAsync();
        }
        
        /// <summary>
        /// Tests the connection settings with the service
        /// </summary>
        /// <param name="settings">The connection settings to test</param>
        /// <returns>True if the connection test passed</returns>
        public async Task<bool> TestConnectionSettingsAsync(ConnectionSettings settings)
        {
            return await _pipeClient.SendCommandAsync("TEST_CONNECTION", settings);
        }
        
        /// <summary>
        /// Sends a request to the service to start a file transfer job
        /// </summary>
        /// <param name="settings">The connection settings to use</param>
        /// <returns>True if the transfer was successfully initiated</returns>
        public async Task<bool> StartTransferAsync(ConnectionSettings settings)
        {
            return await _pipeClient.SendCommandAsync("START_TRANSFER", settings);
        }
        
        /// <summary>
        /// Sends a message to the service and returns the response
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>The response from the service</returns>
        public async Task<string?> SendMessageAsync(string message)
        {
            return await _pipeClient.SendMessageAsync(message);
        }
    }
}
