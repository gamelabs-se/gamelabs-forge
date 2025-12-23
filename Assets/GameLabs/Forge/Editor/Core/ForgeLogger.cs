using UnityEngine;

namespace GameLabs.Forge.Editor
{
    /// <summary>
    /// Logging utility for Forge with debug mode support.
    /// Prefixes all messages with "[Forge]" for easy filtering in the Unity console.
    /// </summary>
    public static class ForgeLogger
    {
        private const string Prefix = "[Forge] ";
        private static bool? _debugMode;
        
        /// <summary>Gets or sets debug mode. When disabled, only errors and warnings are logged.</summary>
        public static bool DebugMode
        {
            get
            {
                if (!_debugMode.HasValue)
                {
                    _debugMode = ForgeConfig.GetDebugMode();
                }
                return _debugMode.Value;
            }
            set => _debugMode = value;
        }
        
        /// <summary>Logs an informational message (only in debug mode).</summary>
        /// <param name="msg">The message to log.</param>
        public static void Log(string msg)
        {
            if (DebugMode)
                UnityEngine.Debug.Log(Prefix + msg);
        }
        
        /// <summary>Logs a debug message (only in debug mode).</summary>
        /// <param name="msg">The debug message to log.</param>
        public static void DebugLog(string msg)
        {
            if (DebugMode)
                UnityEngine.Debug.Log(Prefix + "[DEBUG] " + msg);
        }
        
        /// <summary>Logs a warning message (always shown).</summary>
        /// <param name="msg">The warning message to log.</param>
        public static void Warn(string msg) => UnityEngine.Debug.LogWarning(Prefix + msg);
        
        /// <summary>Logs an error message (always shown).</summary>
        /// <param name="msg">The error message to log.</param>
        public static void Error(string msg) => UnityEngine.Debug.LogError(Prefix + msg);
        
        /// <summary>Logs an error message with additional body text (always shown).</summary>
        /// <param name="msg">The error message header.</param>
        /// <param name="body">Additional error details.</param>
        public static void ErrorFull(string msg, string body) =>
            UnityEngine.Debug.LogError(Prefix + msg + "\n" + body);
        
        /// <summary>Logs a success message (always shown for user feedback).</summary>
        /// <param name="msg">The success message to log.</param>
        public static void Success(string msg) => UnityEngine.Debug.Log(Prefix + "✓ " + msg);
    }
}
