using System;
using System.Collections.Generic;

namespace POGOLib.Logging
{
    /// <summary>
    /// This class contains the configuration for the POGOLib logger.
    /// </summary>
    public static class LoggerConfiguration
    {

        /// <summary>
        ///     Gets or sets the <see cref="MinimumLogLevel" /> for the <see cref="Logger"/>.
        /// </summary>
        public static LogLevel MinimumLogLevel { get; set; } = LogLevel.Debug;

        /// <summary>
        ///     Gets or sets the <see cref="DefaultForegroundColor" /> for the <see cref="Logger"/>.
        /// </summary>
        public static ConsoleColor DefaultForegroundColor = ConsoleColor.Gray;

        /// <summary>
        ///     Gets or sets the <see cref="DefaultBackgroundColor" /> for the <see cref="Logger"/>.
        /// </summary>
        public static ConsoleColor DefaultBackgroundColor = ConsoleColor.Black;

        /// <summary>
        ///     Log colors.
        /// </summary>
        public static readonly Dictionary<LogLevel, LogColor> LogLevelColors = new Dictionary<LogLevel, LogColor>
        {
            {
                LogLevel.Debug,
                new LogColor
                {
                    ForegroundColor = ConsoleColor.DarkGreen,
                    BackgroundColor = ConsoleColor.Black
                }
            },
            {
                LogLevel.Info,
                new LogColor
                {
                    ForegroundColor = ConsoleColor.DarkCyan,
                    BackgroundColor = ConsoleColor.Black
                }
            },
            {
                LogLevel.Notice,
                new LogColor
                {
                    ForegroundColor = ConsoleColor.DarkMagenta,
                    BackgroundColor = ConsoleColor.Black
                }
            },
            {
                LogLevel.Warn,
                new LogColor
                {
                    ForegroundColor = ConsoleColor.DarkYellow,
                    BackgroundColor = ConsoleColor.Black
                }
            },
            {
                LogLevel.Error,
                new LogColor
                {
                    ForegroundColor = ConsoleColor.Red,
                    BackgroundColor = ConsoleColor.Black
                }
            }
        };

    }
}
