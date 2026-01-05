using System;

namespace LoadOrderKeeper.Services
{
    /// <summary>
    /// Provides user-friendly date and time formatting utilities.
    /// </summary>
    public static class DateTimeFormattingService
    {
        /// <summary>
        /// Formats a DateTime into a user-friendly string.
        /// Today: "Today HH:mm"
        /// Yesterday: "Yesterday HH:mm"
        /// This year: "MMM d HH:mm"
        /// Previous years: "MMM d, yyyy HH:mm"
        /// </summary>
        public static string FormatFriendly(DateTime dateTime)
        {
            var now = DateTime.Now;
            var today = now.Date;
            var yesterday = today.AddDays(-1);
            var dateOnly = dateTime.Date;
            var timeString = dateTime.ToString("HH:mm");

            if (dateOnly == today)
            {
                return $"Today {timeString}";
            }

            if (dateOnly == yesterday)
            {
                return $"Yesterday {timeString}";
            }

            // If this year, omit the year
            if (dateTime.Year == now.Year)
            {
                return $"{dateTime:MMM d} {timeString}";
            }

            // Different year, include year
            return $"{dateTime:MMM d, yyyy} {timeString}";
        }

        /// <summary>
        /// Formats a DateTime for status messages with timestamp.
        /// Format: "HH:mm:ss"
        /// </summary>
        public static string FormatTimestamp(DateTime dateTime)
        {
            return dateTime.ToString("HH:mm:ss");
        }

        /// <summary>
        /// Formats a DateTime in ISO 8601 format for technical/log purposes.
        /// Format: "yyyy-MM-dd HH:mm:ss"
        /// </summary>
        public static string FormatIso(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
