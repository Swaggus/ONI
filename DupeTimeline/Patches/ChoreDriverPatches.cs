using HarmonyLib;

namespace DupeTimeline.Patches {
    // Hook A. Chore start/end signal for every chore type, every dupe.
    //
    // Approach: postfix ChoreDriver.States.InitializeStates and inject
    // Enter/Exit callbacks on the haschore state. Same pattern FastTrack
    // uses in PathPatches/NavPatches.cs, so it is known to be perf-safe.
    //
    // Inside the callback, smi is a ChoreDriver.StatesInstance and exposes:
    //   smi.GetCurrentChore() -> Chore (the active chore for this dupe)
    //   smi.gameObject        -> the dupe
    //   smi.navigator         -> Navigator (auto-property)
    //
    // We use smi.GetCurrentChore() directly rather than going through
    // smi.choreConsumer.choreDriver — choreConsumer is a private field on
    // StatesInstance and accessing it from outside PLib's namespace requires
    // IgnoresAccessChecksTo or AccessTools.
    public static class ChoreDriverPatches {
        [HarmonyPatch(typeof(ChoreDriver.States), nameof(ChoreDriver.States.InitializeStates))]
        public static class ChoreDriver_States_InitializeStates_Patch {
            internal static void Postfix(ChoreDriver.States __instance) {
                __instance.haschore.Enter(smi => {
                    var dupe = smi.gameObject;
                    var chore = smi.GetCurrentChore();
                    if (dupe == null || chore == null) return;
                    TimelineStore.OnChoreStart(InstanceIdOf(dupe), chore);
                });
                __instance.haschore.Exit(smi => {
                    var dupe = smi.gameObject;
                    var chore = smi.GetCurrentChore();
                    if (dupe == null) return;
                    TimelineStore.OnChoreEnd(InstanceIdOf(dupe), chore);
                });
            }

            private static int InstanceIdOf(UnityEngine.GameObject go) {
                var id = go.GetComponent<KPrefabID>();
                return id != null ? id.InstanceID : go.GetInstanceID();
            }
        }
    }
}
