using System;
using System.Collections.Concurrent;

namespace POGOLib.Logging
{
    public delegate void LogOutputDelegate(LogLevel logLevel, string message);

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

        static ConcurrentBag<LogOutputDelegate> _logOutputs = new ConcurrentBag<LogOutputDelegate>();

        public static void RegisterLogOutput(LogOutputDelegate logOutput)
        {
            _logOutputs.Add(logOutput);
        }

        private static void Output(LogLevel logLevel, string message)
        {
            foreach(var outputPutter in _logOutputs)
            {
                outputPutter(logLevel, message);
            }

        }

    }
}
