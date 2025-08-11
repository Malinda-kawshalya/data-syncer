using DataSyncer.Core.Models;
using DataSyncer.Core.Utilities;
using System;
using System.IO;

namespace DataSyncer.Core.Services
{
    public static class ConfigurationManager
    {
        private static readonly string ConfigFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "DataSyncer");
        
        private static readonly string ConnectionSettingsPath = Path.Combine(ConfigFolder, "connection.json");
        private static readonly string FilterSettingsPath = Path.Combine(ConfigFolder, "filters.json");
        private static readonly string ScheduleSettingsPath = Path.Combine(ConfigFolder, "schedule.json");

        static ConfigurationManager()
        {
            if (!Directory.Exists(ConfigFolder))
                Directory.CreateDirectory(ConfigFolder);
        }

        // Connection Settings
        public static void SaveConnectionSettings(ConnectionSettings settings)
        {
            JsonFileHelper.SaveToFile(settings, ConnectionSettingsPath);
        }

        public static ConnectionSettings? LoadConnectionSettings()
        {
            return JsonFileHelper.LoadFromFile<ConnectionSettings>(ConnectionSettingsPath);
        }

        // Filter Settings
        public static void SaveFilterSettings(FilterSettings settings)
        {
            JsonFileHelper.SaveToFile(settings, FilterSettingsPath);
        }

        public static FilterSettings? LoadFilterSettings()
        {
            return JsonFileHelper.LoadFromFile<FilterSettings>(FilterSettingsPath);
        }

        // Schedule Settings
        public static void SaveScheduleSettings(ScheduleSettings settings)
        {
            JsonFileHelper.SaveToFile(settings, ScheduleSettingsPath);
        }

        public static ScheduleSettings? LoadScheduleSettings()
        {
            return JsonFileHelper.LoadFromFile<ScheduleSettings>(ScheduleSettingsPath);
        }

        public static string GetConfigFolderPath() => ConfigFolder;
    }
}
