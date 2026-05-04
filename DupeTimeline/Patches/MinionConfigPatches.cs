using HarmonyLib;
using UnityEngine;

namespace DupeTimeline.Patches {
    // Attaches the per-dupe TimelineTracker KMonoBehaviour to every dupe on
    // spawn so its ring-buffer state survives save/load.
    //
    // MinionConfig.OnSpawn signature: `public void OnSpawn(GameObject go)`
    // (instance method). Harmony binds parameters by name, so the postfix
    // parameter MUST be named `go`.
    //
    // TODO: MinionConfig.OnSpawn fires only for vanilla minions. For Bionic
    // dupe support add a parallel patch on BaseMinionConfig.BaseMinion
    // Postfix that does `__result.AddOrGet<TimelineTracker>()`.
    public static class MinionConfigPatches {
        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.OnSpawn))]
        public static class MinionConfig_OnSpawn_Patch {
            internal static void Postfix(GameObject go) {
                if (go == null) return;
                Safe.Run(() => go.AddOrGet<TimelineTracker>());
            }
        }
    }
}
