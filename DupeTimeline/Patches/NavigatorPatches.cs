using HarmonyLib;

namespace DupeTimeline.Patches {
    // Hook C. "Movement segment ended" signal. Useful for chores with no
    // Workable target (mingle, idle moves, schedule transitions) where
    // Hook B never fires.
    //
    // arrived_at_destination distinguishes a clean arrival from an interrupt.
    // The parameter name must match exactly — Harmony binds parameters by name.
    public static class NavigatorPatches {
        [HarmonyPatch(typeof(Navigator), nameof(Navigator.Stop))]
        public static class Navigator_Stop_Patch {
            internal static void Postfix(Navigator __instance, bool arrived_at_destination) {
                if (__instance == null) return;
                var id = __instance.GetComponent<KPrefabID>();
                if (id == null) return;
                TimelineStore.OnNavigatorStop(id.InstanceID, arrived_at_destination);
            }
        }
    }
}
