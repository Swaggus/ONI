using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace DupeTimeline.Debug {
    // F10: dump every dupe's recent timeline to Player.log. Useful sanity
    // check before / instead of the side-screen UI.
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
                int start = System.Math.Max(0, segments.Count - 20);
                for (int i = start; i < segments.Count; i++) {
                    var seg = segments[i];
                    sb.AppendFormat("  {0,-6} {1,5:F1}s  {2,-22} {3}\n",
                        seg.Kind,
                        seg.Duration,
                        seg.ChoreTypeName ?? seg.ChoreTypeId ?? "?",
                        seg.WorkableName ?? "");
                }
            }
            Log.Info(sb.ToString());
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
}
