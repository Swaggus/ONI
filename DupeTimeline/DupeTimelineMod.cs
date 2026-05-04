using HarmonyLib;
using KMod;
using UnityEngine;

namespace DupeTimeline {
    public sealed class DupeTimelineMod : UserMod2 {
        public override void OnLoad(Harmony harmony) {
            base.OnLoad(harmony);
            Log.Info("Loading");
            harmony.PatchAll(typeof(DupeTimelineMod).Assembly);
        }
    }

    // Patches Game.OnSpawn so we can reset the in-memory store for each new
    // save (and tear down the debug-hotkey GameObject from the previous save
    // if there was one). Replaces what PLib's [PLibMethod(RunAt.OnStartGame)]
    // would have done — keeps PLib out of the dependency tree.
    //
    // Game.OnSpawn / OnDestroy are protected, so the Harmony attribute uses
    // string literals instead of nameof() (nameof can't reference symbols
    // that are inaccessible from this assembly).
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
