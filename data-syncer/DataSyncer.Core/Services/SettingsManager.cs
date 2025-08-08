using System;
using System.IO;
using Newtonsoft.Json;
using DataSyncer.Core.Models;
using System.Xml;

namespace DataSyncer.Core.Services
{
    public class SettingsManager
    {
        private readonly string _settingsFilePath;
        private AppSettings _currentSettings;

        public SettingsManager(string settingsFilePath = "appsettings.json")
        {
            _settingsFilePath = settingsFilePath;
            _currentSettings = new AppSettings();
        }

        public AppSettings GetSettings()
        {
            return _currentSettings;
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    _currentSettings = JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings();
                }
                else
                {
                    _currentSettings = new AppSettings();
                    SaveSettings(); // Create default settings file
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load settings: {ex.Message}", ex);
            }
        }

        public void SaveSettings()
        {
            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonConvert.SerializeObject(_currentSettings, Formatting.Indented);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save settings: {ex.Message}", ex);
            }
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            _currentSettings = newSettings ?? throw new ArgumentNullException(nameof(newSettings));
            SaveSettings();
        }

        public bool ValidateSettings(out string validationMessage)
        {
            validationMessage = "";

            // Validate connection settings
            if (string.IsNullOrWhiteSpace(_currentSettings.Connection.Host))
            {
                validationMessage = "Host is required";
                return false;
            }

            if (_currentSettings.Connection.Port <= 0 || _currentSettings.Connection.Port > 65535)
            {
                validationMessage = "Port must be between 1 and 65535";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_currentSettings.Connection.Username))
            {
                validationMessage = "Username is required";
                return false;
            }

            // Validate transfer settings
            if (string.IsNullOrWhiteSpace(_currentSettings.Transfer.LocalPath))
            {
                validationMessage = "Local path is required";
                return false;
            }

            if (!Directory.Exists(_currentSettings.Transfer.LocalPath))
            {
                validationMessage = "Local path does not exist";
                return false;
            }

            // Validate schedule settings
            if (_currentSettings.Schedule.EnableScheduling)
            {
                if (_currentSettings.Schedule.Type == ScheduleType.Interval &&
                    _currentSettings.Schedule.IntervalMinutes <= 0)
                {
                    validationMessage = "Interval must be greater than 0 minutes";
                    return false;
                }

                if (_currentSettings.Schedule.Type == ScheduleType.Weekly &&
                    _currentSettings.Schedule.WeeklyDays.Count == 0)
                {
                    validationMessage = "At least one day must be selected for weekly schedule";
                    return false;
                }
            }

            validationMessage = "Settings are valid";
            return true;
        }
    }
}