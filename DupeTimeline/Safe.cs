using System;

namespace DupeTimeline {
    // Defensive wrapper for hook callbacks. ONI's chore system runs on the
    // sim thread; an unhandled exception in a Harmony patch can take down the
    // whole simulation with no useful stack trace. We catch, log, and after
    // ErrorThreshold failures stop logging to keep Player.log readable.
    //
    // Pattern lifted from PeterHan's EfficientFetch (errorCount + threshold).
    internal static class Safe {
        private const int ErrorThreshold = 10;
        private static int errorCount;

        public static void Run(Action a) {
            if (errorCount >= ErrorThreshold) {
                try { a(); } catch { /* silently swallow once over threshold */ }
                return;
            }
            try {
                a();
            } catch (Exception e) {
                errorCount++;
                Log.Exc(e);
                if (errorCount == ErrorThreshold) {
                    Log.Warn("error threshold reached; suppressing further hook exceptions");
                }
            }
        }
    }
}
