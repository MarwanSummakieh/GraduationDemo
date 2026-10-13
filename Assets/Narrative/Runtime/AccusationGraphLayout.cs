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
            string target, string search = "", string suspectFilter = "", IReadOnlyDictionary<string, string> sockets = null)
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
            graph.nodes.RemoveAll(n => !visible.Contains(n.id)); graph.size = new Vector2(1850, graph.size.y);
            sockets = sockets ?? AccusationReasoning.Allocate(catalogue, tethered);
            for (int i = 0; i < AccusationReasoning.Roles.Length; i++)
            {
                string role = AccusationReasoning.Roles[i]; sockets.TryGetValue(role, out var theoryId);
                graph.nodes.Add(new ReasoningNode { id = AccusationReasoning.SocketId(role), kind = ReasoningNodeKind.AccusationSocket,
                    label = AccusationReasoning.Label(role), speaker = role,
                    detail = "Click a theory, then this " + AccusationReasoning.Label(role) + " socket to attach it. One theory per socket; repeat to untether.\n\n" +
                        (theoryId == null ? "Empty socket." : catalogue.Theory(theoryId)?.text), position = new Vector2(1190, graph.size.y * .5f + (i - 1) * 140) });
            }
            graph.nodes.Add(new ReasoningNode { id = DraftId, kind = ReasoningNodeKind.Accusation, label = "Accusation " + tethered.Count + "/3",
                detail = "DRAFT: " + tethered.Count + "/3 theory tethers. " + (string.IsNullOrEmpty(target) ? "Tether three theories and a suspect." : "Target: " + target + ". Review, then confirm.") +
                    "\n\nAttach one theory to each socket: Means, Motive and Opportunity. Click a theory, then its socket. Repeat to untether.\n\nAfter three theories, click this accusation node and then a suspect node. Review the hypothesis, then confirm.\n\n" +
                    string.Join("\n", tethered.Select(id => catalogue.Theory(id)?.text)) + (string.IsNullOrEmpty(target) ? "" : "\nSuspect: " + target), position = new Vector2(1400, graph.size.y * .5f) });
            var suspects = new[] { "Felix", "Mila", "Bellamy" };
            for (int i = 0; i < suspects.Length; i++)
            {
                string suspect = suspects[i];
                var statements = progress.statements.Select(catalogue.Statement).Where(s => s != null && s.speaker == suspect).ToList();
                graph.nodes.Add(new ReasoningNode { id = SuspectId(suspect), kind = ReasoningNodeKind.Suspect, speaker = suspect, label = suspect,
                    detail = "Click the accusation node, then " + suspect + " to tether your case to this suspect.\n\nCollected statements:\n" +
                        (statements.Count == 0 ? "None collected." : string.Join("\n\n", statements.Select(s => s.claim))),
                    position = new Vector2(1680, graph.size.y * (i + 1) / 4) });
            }
            return graph;
        }
    }
}
