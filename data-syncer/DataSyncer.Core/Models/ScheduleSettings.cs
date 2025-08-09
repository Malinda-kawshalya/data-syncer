using System;

namespace DataSyncer.Core.Models
{
    public class ScheduleSettings
    {
        public DateTime StartTime { get; set; }
        public TimeSpan Interval { get; set; }
        public bool IsEnabled { get; set; }
    }
}
