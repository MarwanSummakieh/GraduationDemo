using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    public static class AccusationGraphLayout
    {
        public const string DraftId = "accusation/draft";
        public static string SuspectId(string suspect) => "suspect/" + suspect;
        public static ReasoningGraphLayout Build(NarrativeCatalogue catalogue, InvestigationProgress progress, IReadOnlyList<string> tethered,
            string target, string search = "", string suspectFilter = "")
        {
            var visible = new HashSet<string>(ReasoningGraphLayout.Build(catalogue, progress, search, suspectFilter).nodes.Select(n => n.id));
            foreach (var id in tethered)
            {
                var theory = catalogue.Theory(id);
                if (theory == null || !progress.theories.Contains(id)) continue;
                visible.Add(id); visible.Add(theory.clueId); visible.Add(theory.statementId);
            }
            // Use the full layout's positions so filtering cannot collide existing or tethered theories.
            var graph = ReasoningGraphLayout.Build(catalogue, progress);
            graph.nodes.RemoveAll(n => !visible.Contains(n.id)); graph.size = new Vector2(1730, graph.size.y);
            graph.nodes.Add(new ReasoningNode { id = DraftId, kind = ReasoningNodeKind.Accusation, label = "Accusation " + tethered.Count + "/3",
                detail = "DRAFT: " + tethered.Count + "/3 theory tethers. " + (string.IsNullOrEmpty(target) ? "Tether three theories and a suspect." : "Target: " + target + ". Review, then confirm.") +
                    "\n\nConnect theories with accusation-tethers to build an accusation. Click a theory, then this accusation node. Repeat to untether.\n\nAfter three theories, click this node and then a suspect node to tether your accusation. Review it, then confirm.\n\n" +
                    string.Join("\n", tethered.Select(id => catalogue.Theory(id)?.text)) + (string.IsNullOrEmpty(target) ? "" : "\nSuspect: " + target), position = new Vector2(1280, graph.size.y * .5f) });
            var suspects = new[] { "Felix", "Mila", "Bellamy" };
            for (int i = 0; i < suspects.Length; i++)
            {
                string suspect = suspects[i];
                var statements = progress.statements.Select(catalogue.Statement).Where(s => s != null && s.speaker == suspect).ToList();
                graph.nodes.Add(new ReasoningNode { id = SuspectId(suspect), kind = ReasoningNodeKind.Suspect, speaker = suspect, label = suspect,
                    detail = "Click the accusation node, then " + suspect + " to tether your case to this suspect.\n\nCollected statements:\n" +
                        (statements.Count == 0 ? "None collected." : string.Join("\n\n", statements.Select(s => s.claim))),
                    position = new Vector2(1560, graph.size.y * (i + 1) / 4) });
            }
            return graph;
        }
    }
}
