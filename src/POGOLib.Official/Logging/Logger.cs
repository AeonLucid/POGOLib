using System.Collections.Generic;

namespace POGOLib.Official.Logging
{
    public delegate void LogOutputDelegate(LogLevel logLevel, string message);

    public static class Logger
    {
        private static readonly List<LogOutputDelegate> LogOutputs = new List<LogOutputDelegate>();

        public static void RegisterLogOutput(LogOutputDelegate logOutput)
        {
            LogOutputs.Add(logOutput);
        }

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
            foreach(var logOutput in LogOutputs)
            {
                logOutput(logLevel, message);
            }
        }
    }
}
