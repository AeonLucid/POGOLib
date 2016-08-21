using System;

namespace POGOLib.Logging
{
    public static class Logger
    {
		public static void Debug(string message, params object[] args)
        {
			Output(LogLevel.Debug, string.Format(message, args));
        }

		public static void Info(string message, params object[] args)
        {
			Output(LogLevel.Info, string.Format(message, args));
        }

		public static void Notice(string message, params object[] args)
        {
			Output(LogLevel.Notice, string.Format(message, args));
        }

		public static void Warn(string message, params object[] args)
        {
			Output(LogLevel.Warn, string.Format(message, args));
        }

		public static void Error(string message, params object[] args)
        {
			Output(LogLevel.Error, string.Format(message, args));
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
			Console.WriteLine(string.Format("{0,-10}{1,-8}{2}", timestamp, logLevel, message));
            Console.ResetColor();
        }
    }
}
