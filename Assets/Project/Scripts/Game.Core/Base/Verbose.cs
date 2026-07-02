using System;
using System.Diagnostics;
using static UnityEngine.Debug;

namespace Game.Core.Base
{
    public static class Verbose
    {
        public enum LogLevel
        {
            Info,
            Warning,
            Error,
            Exception
        }

        private static LogLevel _currentLogLevel = LogLevel.Info;

        public static void SetLogLevel(LogLevel level) => _currentLogLevel = level;
        
        [Conditional("FRAMEWORK_DEBUG")]
        public static void D(string message)
        {
            if (_currentLogLevel <= LogLevel.Info) Log($"<color=#3498db>[INFO]</color> {message}");
        }

        [Conditional("FRAMEWORK_DEBUG")]
        public static void W(string message)
        {
            if (_currentLogLevel <= LogLevel.Warning) LogWarning($"<color=#f39c12>[WARNING]</color> {message}");
        }

        public static void E(string message)
        {
            if (_currentLogLevel <= LogLevel.Error) LogError($"<color=#e74c3c>[ERROR]</color> {message}");
        }

        public static void Ex(Exception exception) => LogException(exception);
    }
}
