using System;
using UnityEngine;

namespace DupeTimeline {
    internal static class Log {
        private const string Prefix = "[DupeTimeline] ";
        public static void Info(string msg) { Debug.Log(Prefix + msg); }
        public static void Warn(string msg) { Debug.LogWarning(Prefix + msg); }
        public static void Exc(Exception e) { Debug.LogException(e); }
    }
}
