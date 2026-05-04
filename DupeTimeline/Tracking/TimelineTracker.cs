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
    public sealed class TimelineTracker : KMonoBehaviour {
        [Serialize]
        private List<SerializedSegment> persistedSegments = new List<SerializedSegment>();

        protected override void OnSpawn() {
            base.OnSpawn();
            // TODO: rehydrate TimelineStore's ring buffer for this dupe from
            // persistedSegments. Resolve our dupe instance id via
            // GetComponent<KPrefabID>().InstanceID.
        }

        protected override void OnCleanUp() {
            // TODO: snapshot live ring buffer back into persistedSegments
            // before destruction so OnSerializing has fresh data.
            base.OnCleanUp();
        }

        // KSerialization hook fired immediately before this component is
        // written to the save. Snapshot the in-memory ring buffer here.
        public void OnSerializing() {
            // TODO: copy TimelineStore.GetTimeline(InstanceId) into
            // persistedSegments.
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
