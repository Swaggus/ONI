using HarmonyLib;
using KMod;
using PeterHan.PLib.Core;
using UnityEngine;

namespace DupeTimeline {
    public sealed class DupeTimelineMod : UserMod2 {
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            Log.Info("Loading");
            // PLib is ILRepacked into our DLL with all types internalized.
            // PUtil.InitLibrary boots the registry, sets up shared services,
            // and is safe to call multiple times.
            PUtil.InitLibrary();
            harmony.PatchAll(typeof(DupeTimelineMod).Assembly);
        }
    }

    // Patches Game.OnSpawn / OnDestroy via string literals because the
    // methods are protected and inaccessible from this assembly's namespace.
    [HarmonyPatch(typeof(Game), "OnSpawn")]
    internal static class Game_OnSpawn_Patch {
        private static GameObject debugGo;

        internal static void Postfix() {
            TimelineStore.Reset();

            if (debugGo != null) Object.Destroy(debugGo);
            debugGo = new GameObject("DupeTimelineDebug");
            debugGo.AddComponent<Diagnostics.DebugDumpHotkey>();
            Object.DontDestroyOnLoad(debugGo);

            Log.Info("Started (press F10 to dump timelines)");
        }
    }

    [HarmonyPatch(typeof(Game), "OnDestroy")]
    internal static class Game_OnDestroy_Patch {
        internal static void Prefix() {
            TimelineStore.Reset();
        }
    }
}
