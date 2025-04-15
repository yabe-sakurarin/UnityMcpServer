using UnityEngine;

namespace Sakurarin.UnityMcpServer.Runtime.Core
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warn,
        Error
    }

    public static class LoggingService
    {
        private static LogLevel _currentLogLevel = LogLevel.Info;
        private const string LogPrefix = "[UnityMcpServer] ";

        public static void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
            Log(LogLevel.Info, $"Log level set to: {level}");
        }

        public static void Log(LogLevel level, string message)
        {
            if (level >= _currentLogLevel)
            {
                string formattedMessage = $"{LogPrefix}[{level.ToString().ToUpper()}] {message}";
                switch (level)
                {
                    case LogLevel.Debug:
                    case LogLevel.Info:
                        Debug.Log(formattedMessage);
                        break;
                    case LogLevel.Warn:
                        Debug.LogWarning(formattedMessage);
                        break;
                    case LogLevel.Error:
                        Debug.LogError(formattedMessage);
                        break;
                }
            }
        }

        public static void LogDebug(string message) => Log(LogLevel.Debug, message);
        public static void LogInfo(string message) => Log(LogLevel.Info, message);
        public static void LogWarn(string message) => Log(LogLevel.Warn, message);
        public static void LogError(string message) => Log(LogLevel.Error, message);
    }
} 