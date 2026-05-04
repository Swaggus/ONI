namespace DupeTimeline {
    // Fully-qualified UnityEngine.Debug to avoid any future namespace
    // shadowing (ONI has its own Debug-adjacent symbols floating around).
    internal static class Log {
        private const string Prefix = "[DupeTimeline] ";
        public static void Info(string msg) { UnityEngine.Debug.Log(Prefix + msg); }
        public static void Warn(string msg) { UnityEngine.Debug.LogWarning(Prefix + msg); }
        public static void Exc(System.Exception e) { UnityEngine.Debug.LogException(e); }
    }
}
