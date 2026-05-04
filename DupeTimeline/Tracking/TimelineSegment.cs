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
        public int WorkableInstanceId;
        public int TargetCell;

        public float Duration => EndTime - StartTime;
    }
}
