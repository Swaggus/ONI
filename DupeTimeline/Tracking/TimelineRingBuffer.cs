using System.Collections.Generic;

namespace DupeTimeline {
    public sealed class TimelineRingBuffer {
        private readonly TimelineSegment[] buffer;
        private int head;
        private int count;

        public int Capacity { get; }
        public int Count => count;

        public TimelineRingBuffer(int capacity) {
            Capacity = capacity;
            buffer = new TimelineSegment[capacity];
        }

        public void Add(TimelineSegment segment) {
            buffer[head] = segment;
            head = (head + 1) % Capacity;
            if (count < Capacity) count++;
        }

        public IEnumerable<TimelineSegment> InOrder() {
            int start = count < Capacity ? 0 : head;
            for (int i = 0; i < count; i++) {
                yield return buffer[(start + i) % Capacity];
            }
        }

        public void Clear() {
            for (int i = 0; i < buffer.Length; i++) buffer[i] = null;
            head = 0;
            count = 0;
        }
    }
}
