using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DupeTimeline.UI {
    // Registers the per-dupe timeline panel with DetailsScreen so it appears
    // in the right-hand strip when a dupe is selected.
    //
    // Pattern (PLib-free): patch DetailsScreen.OnPrefabInit Postfix, build a
    // disabled GameObject hosting our SideScreenContent, parent under the
    // DetailsScreen itself, and append a SideScreenRef to the screen list.
    //
    // We don't need PLib's PUIUtils.GetSideScreenContent — DetailsScreen
    // re-parents side-screen instances into its own content area when they
    // become active. Initial parenting under __instance.transform is enough
    // to keep the GameObject alive and discoverable.
    public static class DupeTimelineSideScreenPatch {
        [HarmonyPatch(typeof(DetailsScreen), "OnPrefabInit")]
        public static class DetailsScreen_OnPrefabInit_Patch {
            internal static void Postfix(DetailsScreen __instance) {
                Safe.Run(() => Register(__instance));
            }

            private static void Register(DetailsScreen detailsScreen) {
                var sideScreensField = AccessTools.Field(typeof(DetailsScreen), "sideScreens");
                if (sideScreensField == null) {
                    Log.Warn("DetailsScreen.sideScreens field not found; side-screen disabled");
                    return;
                }
                var sideScreens = sideScreensField.GetValue(detailsScreen)
                    as List<DetailsScreen.SideScreenRef>;
                if (sideScreens == null) {
                    Log.Warn("DetailsScreen.sideScreens not a List<SideScreenRef>; side-screen disabled");
                    return;
                }

                var go = new GameObject(nameof(DupeTimelineSideScreen));
                go.SetActive(false);
                go.transform.SetParent(detailsScreen.transform, false);
                go.transform.localScale = Vector3.one;

                var screen = go.AddComponent<DupeTimelineSideScreen>();

                sideScreens.Add(new DetailsScreen.SideScreenRef {
                    name = nameof(DupeTimelineSideScreen),
                    offset = Vector2.zero,
                    screenPrefab = screen,
                    screenInstance = screen,
                });
            }
        }
    }
}
