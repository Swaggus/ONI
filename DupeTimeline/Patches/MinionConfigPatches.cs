using HarmonyLib;
using UnityEngine;

namespace DupeTimeline.Patches {
    // Attaches the per-dupe TimelineTracker KMonoBehaviour to every dupe on
    // spawn so its ring-buffer state survives save/load.
    //
    // SIGNATURE TO VERIFY: MinionConfig.OnSpawn — depending on game version
    // this is either a static method `static GameObject OnSpawn(GameObject inst)`
    // or attached differently. If the patch fails to apply, switch to patching
    // BaseMinionConfig.OnSpawn or BaseMinionConfig.BaseMinion.
    public static class MinionConfigPatches {
        [HarmonyPatch(typeof(MinionConfig), nameof(MinionConfig.OnSpawn))]
        public static class MinionConfig_OnSpawn_Patch {
            internal static void Postfix(GameObject inst) {
                if (inst == null) return;
                inst.AddOrGet<TimelineTracker>();
            }
        }
    }
}
