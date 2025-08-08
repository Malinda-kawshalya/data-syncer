using System;
using System.Collections.Generic;

namespace DataSyncer.Core.Models
{
    public class AppSettings
    {
        public ConnectionSettings Connection { get; set; } = new ConnectionSettings();
        public TransferSettings Transfer { get; set; } = new TransferSettings();
        public ScheduleSettings Schedule { get; set; } = new ScheduleSettings();
        public LoggingSettings Logging { get; set; } = new LoggingSettings();
        public UISettings UI { get; set; } = new UISettings();
    }

    public class ConnectionSettings
    {
        public string Protocol { get; set; } = "SFTP"; // FTP or SFTP
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22; // 22 for SFTP, 21 for FTP
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UsePassiveMode { get; set; } = true; // For FTP
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class TransferSettings
    {
        public string LocalPath { get; set; } = "";
        public string RemotePath { get; set; } = "/";
        public TransferDirection Direction { get; set; } = TransferDirection.Upload;
        public List<string> FileFilters { get; set; } = new List<string> { "*.*" };
        public bool IncludeSubfolders { get; set; } = false;
        public PostTransferAction PostAction { get; set; } = PostTransferAction.None;
        public string ArchivePath { get; set; } = "";
        public bool OverwriteExisting { get; set; } = true;
    }

    public class ScheduleSettings
    {
        public bool EnableScheduling { get; set; } = false;
        public ScheduleType Type { get; set; } = ScheduleType.Interval;
        public int IntervalMinutes { get; set; } = 10;
        public TimeSpan DailyTime { get; set; } = new TimeSpan(9, 0, 0); // 9:00 AM
        public List<DayOfWeek> WeeklyDays { get; set; } = new List<DayOfWeek>();
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime? EndDate { get; set; } = null;
    }

    public class LoggingSettings
    {
        public bool EnableLogging { get; set; } = true;
        public LogLevel LogLevel { get; set; } = LogLevel.Info;
        public string LogFilePath { get; set; } = "logs/datasyncer.log";
        public int MaxLogSizeMB { get; set; } = 10;
        public int MaxLogFiles { get; set; } = 5;
    }

    public class UISettings
    {
        public bool MinimizeToTray { get; set; } = true;
        public bool StartMinimized { get; set; } = false;
        public bool ShowNotifications { get; set; } = true;
        public bool AutoStartWithWindows { get; set; } = false;
    }

    public enum TransferDirection
    {
        Upload,
        Download,
        Bidirectional
    }

    public enum PostTransferAction
    {
        None,
        Delete,
        Archive
    }

    public enum ScheduleType
    {
        Interval,
        Daily,
        Weekly
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}