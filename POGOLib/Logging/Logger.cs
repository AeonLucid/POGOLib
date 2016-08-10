using System;

namespace POGOLib.Logging
{
    public static class Logger
    {

        public static void Debug(string message)
        {
            Output(LogLevel.Debug, message);
        }

        public static void Info(string message)
        {
            Output(LogLevel.Info, message);
        }

        public static void Notice(string message)
        {
            Output(LogLevel.Notice, message);
        }

        public static void Warn(string message)
        {
            Output(LogLevel.Warn, message);
        }

        public static void Error(string message)
        {
            Output(LogLevel.Error, message);
        }

        private static void Output(LogLevel logLevel, string message)
        {
            if (logLevel < LoggerConfiguration.MinimumLogLevel) return;

            var foregroundColor = LoggerConfiguration.DefaultForegroundColor;
            var backgroundColor = LoggerConfiguration.DefaultBackgroundColor;
            var timestamp = DateTime.Now.ToString("HH:mm:ss");

            if (LoggerConfiguration.LogLevelColors.ContainsKey(logLevel))
            {
                var colors = LoggerConfiguration.LogLevelColors[logLevel];

                foregroundColor = colors.ForegroundColor;
                backgroundColor = colors.BackgroundColor;
            }

            Console.ForegroundColor = foregroundColor;
            Console.BackgroundColor = backgroundColor;
            Console.WriteLine($"{timestamp,-10}{logLevel,-8}{message}");
            Console.ResetColor();
        }

    }
}
