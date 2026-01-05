using System;
using LoadOrderKeeper.Services;

namespace LoadOrderKeeper.Models
{
    public sealed class StatusMessageModel
    {
        public StatusMessageModel(string message, DateTime timestamp, StatusMessageType type = StatusMessageType.Info)
        {
            Message = message;
            Timestamp = timestamp;
            Type = type;
        }

        public string Message { get; }
        public DateTime Timestamp { get; }
        public StatusMessageType Type { get; }
        
        public string FormattedTimestamp => DateTimeFormattingService.FormatTimestamp(Timestamp);
        
        public string DisplayText => $"[{FormattedTimestamp}] {Message}";
    }

    public enum StatusMessageType
    {
        Info,
        Success,
        Warning,
        Error
    }
}