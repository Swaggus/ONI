namespace DupeTimeline {
    public enum TimelineSegmentKind {
        Travel,
        Work,
        Idle,
    }

    public sealed class TimelineSegment {
        public float StartTime;
        public float EndTime;
        public TimelineSegmentKind Kind;
        public string ChoreGuid;
        public string ChoreTypeId;
        public string ChoreTypeName;     // Resolved at record time (e.g. "Fetch Food")
        public int WorkableInstanceId;
        public string WorkableName;      // Resolved at record time (e.g. "Storage Bin")
        public int TargetCell;

        public float Duration => EndTime - StartTime;
    }
}
