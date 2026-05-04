using HarmonyLib;
using PeterHan.PLib.UI;

namespace DupeTimeline.UI {
    // Registers DupeTimelineSideScreen with DetailsScreen via PLib's
    // PUIUtils.AddSideScreenContent helper. PLib handles parenting,
    // scaling, and the SideScreenRef bookkeeping that we previously did
    // by hand via Traverse / AccessTools.
    public static class DupeTimelineSideScreenPatch {
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreen_OnPrefabInit_Patch {
            internal static void Postfix() {
                Safe.Run(() => PUIUtils.AddSideScreenContent<DupeTimelineSideScreen>());
            }
        }
    }
}
