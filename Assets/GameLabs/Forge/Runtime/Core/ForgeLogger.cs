using UnityEngine;

namespace GameLabs.Forge
{
    public static class ForgeLogger
    {
        const string Prefix = "[Forge] ";
        public static void Log(string msg) => Debug.Log(Prefix + msg);
        public static void Warn(string msg) => Debug.LogWarning(Prefix + msg);
        public static void Error(string msg) => Debug.LogError(Prefix + msg);
        public static void ErrorFull(string msg, string body) =>
            Debug.LogError(Prefix + msg + "\n" + body);
    }
}
