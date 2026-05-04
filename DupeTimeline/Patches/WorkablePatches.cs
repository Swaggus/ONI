using HarmonyLib;

namespace DupeTimeline.Patches {
    // Hook B. Subscribe to Workable.OnWorkableEventCB on every Workable as it
    // spawns. The callback fires with WorkStarted / WorkCompleted / WorkStopped
    // and tells us when the dupe actually arrived at the workplace and started
    // doing the work — the inner edge of the travel/work boundary.
    //
    // We piggyback on Klei's vanilla event so no patches on the work events
    // themselves are needed. OnWorkTick is hot (every sim tick during work)
    // and is deliberately NOT hooked.
    public static class WorkablePatches {
        [HarmonyPatch(typeof(Workable), "OnSpawn")]
        public static class Workable_OnSpawn_Patch {
            internal static void Postfix(Workable __instance) {
                __instance.OnWorkableEventCB += TimelineStore.OnWorkableEvent;
            }
        }
    }
}
