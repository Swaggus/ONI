using System.Collections.Generic;
using HarmonyLib;
using PeterHan.PLib.UI;
using UnityEngine;

namespace DupeTimeline.UI {
    // Registers the per-dupe timeline panel with DetailsScreen so it appears
    // in the right-hand strip when a dupe is selected.
    //
    // Pattern from PeterHan's WorkshopProfiles/WorkshopProfilesPatches.cs:
    //   1. Postfix DetailsScreen.OnPrefabInit
    //   2. Pluck the parent transform via PUIUtils.GetSideScreenContent
    //   3. Build a disabled GameObject hosting our SideScreenContent
    //   4. Reset localScale (cloned/parented prefabs come out at 0.8x)
    //   5. Append (not Insert(0)) to the sideScreens list so vanilla minion
    //      side-screens keep priority — ours just sits alongside them.
    public static class DupeTimelineSideScreenPatch {
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreen_OnPrefabInit_Patch {
            internal static void Postfix(
                    List<DetailsScreen.SideScreenRef> ___sideScreens,
                    DetailsScreen __instance) {
                var parent = PUIUtils.GetSideScreenContent(__instance);
                if (parent == null) return;

                var go = new GameObject(nameof(DupeTimelineSideScreen));
                go.SetActive(false);
                go.transform.SetParent(parent.transform, false);
                go.transform.localScale = Vector3.one;

                var screen = go.AddComponent<DupeTimelineSideScreen>();

                ___sideScreens.Add(new DetailsScreen.SideScreenRef {
                    name = nameof(DupeTimelineSideScreen),
                    offset = Vector2.zero,
                    screenPrefab = screen,
                    screenInstance = screen,
                });
            }
        }
    }
}
