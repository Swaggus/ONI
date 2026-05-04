using System.Collections.Generic;
using System.Linq;
using System.Text;
using PeterHan.PLib.Core;
using UnityEngine;

namespace DupeTimeline.Debug {
    // F10: dump every dupe's recent timeline to Player.log so the mod is
    // testable before the side-screen UI lands. Crude but verifies that the
    // four hooks are firing and producing sensible data.
    public sealed class DebugDumpHotkey : MonoBehaviour {
        private void Update() {
            if (Input.GetKeyDown(KeyCode.F10)) {
                Dump();
            }
        }

        private static void Dump() {
            var sb = new StringBuilder();
            sb.AppendLine("=== DupeTimeline dump ===");

            foreach (var dupeId in TimelineStore.KnownDupeIds().ToList()) {
                var name = NameOf(dupeId);
                var segments = TimelineStore.GetTimeline(dupeId).ToList();
                sb.AppendFormat("[{0} (id {1})] {2} segments\n",
                    name, dupeId, segments.Count);
                foreach (var seg in segments.TakeLast(20)) {
                    sb.AppendFormat("  {0,-6} {1,8:F1}-{2,-8:F1} ({3,5:F1}s) chore={4} workable={5}\n",
                        seg.Kind,
                        seg.StartTime,
                        seg.EndTime,
                        seg.Duration,
                        seg.ChoreTypeId ?? "?",
                        seg.WorkableInstanceId);
                }
            }
            PUtil.LogDebug(sb.ToString());
        }

        private static string NameOf(int instanceId) {
            if (Components.LiveMinionIdentities?.Items == null) return "?";
            foreach (var ident in Components.LiveMinionIdentities.Items) {
                if (ident == null) continue;
                var pid = ident.GetComponent<KPrefabID>();
                if (pid != null && pid.InstanceID == instanceId) {
                    return ident.GetProperName();
                }
            }
            return "?";
        }
    }

    internal static class EnumerableExtensions {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int n) {
            var list = source as IList<T> ?? source.ToList();
            int start = System.Math.Max(0, list.Count - n);
            for (int i = start; i < list.Count; i++) yield return list[i];
        }
    }
}
