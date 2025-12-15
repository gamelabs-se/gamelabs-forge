using UnityEngine;

namespace GameLabs.Forge
{
    /// <summary>
    /// Simple logging utility for Forge messages.
    /// Prefixes all messages with "[Forge]" for easy filtering in the Unity console.
    /// </summary>
    public static class ForgeLogger
    {
        private const string Prefix = "[Forge] ";
        
        /// <summary>Logs an informational message.</summary>
        /// <param name="msg">The message to log.</param>
        public static void Log(string msg) => Debug.Log(Prefix + msg);
        
        /// <summary>Logs a warning message.</summary>
        /// <param name="msg">The warning message to log.</param>
        public static void Warn(string msg) => Debug.LogWarning(Prefix + msg);
        
        /// <summary>Logs an error message.</summary>
        /// <param name="msg">The error message to log.</param>
        public static void Error(string msg) => Debug.LogError(Prefix + msg);
        
        /// <summary>Logs an error message with additional body text.</summary>
        /// <param name="msg">The error message header.</param>
        /// <param name="body">Additional error details.</param>
        public static void ErrorFull(string msg, string body) =>
            Debug.LogError(Prefix + msg + "\n" + body);
    }
}
