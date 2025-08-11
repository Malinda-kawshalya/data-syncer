using DataSyncer.Core.Models;
using System;

namespace DataSyncer.WinFormsUI.Utilities
{
    public class ConnectionManager
    {
        private static readonly Lazy<ConnectionManager> _instance = new Lazy<ConnectionManager>(() => new ConnectionManager());
        public static ConnectionManager Instance => _instance.Value;

        // Current connection settings stored here
        public ConnectionSettings? CurrentSettings { get; private set; }

        // Update settings when user saves them
        public void UpdateSettings(ConnectionSettings settings)
        {
            CurrentSettings = settings;
        }
    }
}
