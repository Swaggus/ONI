using HarmonyLib;
using UnityEngine;

namespace DupeTimeline.Patches {
    // Hook A. Chore start/end signal for every chore type, every dupe.
    //
    // Postfix ChoreDriver.States.InitializeStates and inject Enter/Exit
    // callbacks on the haschore state. Same pattern FastTrack uses.
    //
    // Inside the callback, smi is a ChoreDriver.StatesInstance and exposes:
    //   smi.GetCurrentChore() -> Chore (the active chore for this dupe)
    //   smi.gameObject        -> the dupe
    //   smi.navigator         -> Navigator
    //
    // Both callbacks are wrapped in Safe.Run because they run inside ONI's
    // chore-driver state machine; an uncaught exception here would halt the
    // sim with no stack trace.
    public static class ChoreDriverPatches {
        [HarmonyPatch(typeof(ChoreDriver.States), nameof(ChoreDriver.States.InitializeStates))]
        public static class ChoreDriver_States_InitializeStates_Patch {
            internal static void Postfix(ChoreDriver.States __instance) {
                __instance.haschore.Enter(smi => Safe.Run(() => OnEnter(smi)));
                __instance.haschore.Exit(smi => Safe.Run(() => OnExit(smi)));
            }

            private static void OnEnter(ChoreDriver.StatesInstance smi) {
                var dupe = smi.gameObject;
                var chore = smi.GetCurrentChore();
                if (dupe == null || chore == null) return;
                TimelineStore.OnChoreStart(InstanceIdOf(dupe), chore);
            }

            private static void OnExit(ChoreDriver.StatesInstance smi) {
                var dupe = smi.gameObject;
                if (dupe == null) return;
                TimelineStore.OnChoreEnd(InstanceIdOf(dupe), smi.GetCurrentChore());
            }

            private static int InstanceIdOf(GameObject go) {
                var id = go.GetComponent<KPrefabID>();
                return id != null ? id.InstanceID : go.GetInstanceID();
            }
        }
    }
}
