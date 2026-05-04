using System.Collections.Generic;
using KSerialization;

namespace DupeTimeline {
    // Per-dupe component that survives save/load. Serializes the dupe's
    // recent timeline so the Gantt panel still has data after a save round
    // trip. Live in-flight state is intentionally NOT serialized — chores
    // are reconstructed on load by Klei, so any in-flight chore at save
    // time becomes a fresh chore on load.
    //
    // Pattern lifted from PeterHan's ResourcesInMotion StateTimeTrackerComponent.
    [SerializationConfig(MemberSerialization.OptIn)]
    public sealed class TimelineTracker : KMonoBehaviour, ISaveLoadable {
        [Serialize]
        private List<SerializedSegment> persistedSegments = new List<SerializedSegment>();

        private int InstanceId {
            get {
                var id = GetComponent<KPrefabID>();
                return id != null ? id.InstanceID : -1;
            }
        }

        protected override void OnSpawn() {
            base.OnSpawn();
            var id = InstanceId;
            if (id < 0) return;
            TimelineStore.Restore(id, persistedSegments);
        }

        // Klei's KSerialization fires this immediately before this component
        // is written to the save. Snapshot live ring buffer into the
        // persisted list so [Serialize] picks up fresh data.
        public void OnSerializing() {
            var id = InstanceId;
            if (id < 0) return;
            persistedSegments = TimelineStore.Snapshot(id);
        }
    }

    [SerializationConfig(MemberSerialization.OptIn)]
    public struct SerializedSegment {
        [Serialize] public float StartTime;
        [Serialize] public float EndTime;
        [Serialize] public int Kind;
        [Serialize] public string ChoreGuid;
        [Serialize] public string ChoreTypeId;
        [Serialize] public int WorkableInstanceId;
        [Serialize] public int TargetCell;
    }
}
