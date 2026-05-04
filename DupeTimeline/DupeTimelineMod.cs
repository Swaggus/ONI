using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;
using UnityEngine;

namespace DupeTimeline {
    public sealed class DupeTimelineMod : UserMod2 {
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            PUtil.InitLibrary();

            var pm = new PPatchManager(harmony);
            pm.RegisterPatchClass(typeof(DupeTimelineMod));
            pm.RegisterPatchClass(typeof(Patches.ChoreDriverPatches));
            pm.RegisterPatchClass(typeof(Patches.WorkablePatches));
            pm.RegisterPatchClass(typeof(Patches.NavigatorPatches));
            pm.RegisterPatchClass(typeof(Patches.MinionConfigPatches));
            pm.RegisterPatchClass(typeof(UI.DupeTimelineSideScreenPatch));

            new POptions().RegisterOptions(this, typeof(DupeTimelineOptions));
        }

        private static GameObject debugGo;

        [PLibMethod(RunAt.OnStartGame)]
        internal static void OnStartGame() {
            var options = POptions.ReadSettings<DupeTimelineOptions>()
                ?? new DupeTimelineOptions();
            TimelineStore.Configure(options);
            TimelineStore.Reset();

            // Plain GameObject host for the debug hotkey — no KMonoBehaviour
            // lifecycle needed.
            debugGo = new GameObject("DupeTimelineDebug");
            debugGo.AddComponent<Debug.DebugDumpHotkey>();
            Object.DontDestroyOnLoad(debugGo);

            PUtil.LogDebug("DupeTimeline started (press F10 to dump timelines)");
        }

        [PLibMethod(RunAt.OnEndGame)]
        internal static void OnEndGame() {
            TimelineStore.Reset();
            if (debugGo != null) {
                Object.Destroy(debugGo);
                debugGo = null;
            }
        }
    }
}
