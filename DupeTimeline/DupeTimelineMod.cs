using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;
using PeterHan.PLib.PatchManager;

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

            new POptions().RegisterOptions(this, typeof(DupeTimelineOptions));
        }

        [PLibMethod(RunAt.OnStartGame)]
        internal static void OnStartGame() {
            var options = POptions.ReadSettings<DupeTimelineOptions>()
                ?? new DupeTimelineOptions();
            TimelineStore.Configure(options);
            TimelineStore.Reset();
            PUtil.LogDebug("DupeTimeline started");
        }

        [PLibMethod(RunAt.OnEndGame)]
        internal static void OnEndGame() {
            TimelineStore.Reset();
        }
    }
}
