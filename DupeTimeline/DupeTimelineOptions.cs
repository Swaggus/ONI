using PeterHan.PLib.Options;

namespace DupeTimeline {
    [ModInfo("https://github.com/Swaggus/ONI")]
    [ConfigFile(SharedConfigLocation: true)]
    public sealed class DupeTimelineOptions {
        [Option("Cycles retained",
            "How many recent cycles of activity to keep in memory per dupe.")]
        [Limit(1, 10)]
        public int CyclesRetained { get; set; } = 3;
    }
}
