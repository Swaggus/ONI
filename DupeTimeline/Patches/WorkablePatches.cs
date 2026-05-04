using HarmonyLib;

namespace DupeTimeline.Patches {
    // Hook B. Subscribe to Workable.OnWorkableEventCB on every Workable as it
    // spawns. The callback fires with WorkStarted / WorkCompleted / WorkStopped
    // and tells us when the dupe actually arrived at the workplace and started
    // doing the work — the inner edge of the travel/work boundary.
    public static class WorkablePatches {
        [HarmonyPatch(typeof(Workable), "OnSpawn")]
        public static class Workable_OnSpawn_Patch {
            internal static void Postfix(Workable __instance) {
                __instance.OnWorkableEventCB += OnWorkableEvent;
            }

            private static void OnWorkableEvent(Workable w, Workable.WorkableEvent evt) {
                Safe.Run(() => TimelineStore.OnWorkableEvent(w, evt));
            }
        }
    }
}
