using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    public enum ReasoningNodeKind { Clue, Statement, Theory, Accusation, Suspect, AccusationSocket }
    public sealed class ReasoningNode
    {
        public string id, label, detail, speaker, clueParent, statementParent;
        public ReasoningNodeKind kind;
        public bool anticipated;
        public Vector2 position;
    }
    public sealed class ReasoningGraphLayout
    {
        public readonly List<ReasoningNode> nodes = new List<ReasoningNode>();
        public Vector2 size;
        public static string QuestionLabel(InvestigationProgress progress, string id) => "Question " + (progress.theories.IndexOf(id) + 1);
        public static ReasoningGraphLayout Build(NarrativeCatalogue catalogue, InvestigationProgress progress, string search = "", string suspect = "")
        {
            var model = new ReasoningGraphLayout();
            bool Matches(string text) => string.IsNullOrEmpty(search) || (text ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;
            bool Speaker(string speaker) => string.IsNullOrEmpty(suspect) || suspect == speaker;
            var theories = progress.theories.Select(catalogue.Theory).Where(t => t != null && Matches(t.text) && Speaker(catalogue.Statement(t.statementId)?.speaker)).OrderBy(t => t.id, StringComparer.Ordinal).ToList();
            var clues = progress.clues.Concat(progress.clueHints.Select(h => h.clueId)).Distinct().Select(catalogue.Clue)
                .Where(c => c != null && (Matches(c.name + " " + (progress.clues.Contains(c.id) ? c.fact : c.hint)) || theories.Any(t => t.clueId == c.id)))
                .OrderBy(c => c.id, StringComparer.Ordinal).ToList();
            var statements = progress.statements.Select(catalogue.Statement).Where(s => s != null && Speaker(s.speaker) && (Matches(s.claim + " " + s.speaker) || theories.Any(t => t.statementId == s.id))).OrderBy(s => s.speaker == "Felix" ? 0 : s.speaker == "Mila" ? 1 : 2).ThenBy(s => s.id, StringComparer.Ordinal).ToList();
            // Any visible theory always retains both acquired parents, even under search/filter.
            foreach (var t in theories)
            {
                if (progress.clues.Contains(t.clueId) && !clues.Any(c => c.id == t.clueId)) clues.Add(catalogue.Clue(t.clueId));
                if (progress.statements.Contains(t.statementId) && !statements.Any(s => s.id == t.statementId)) statements.Add(catalogue.Statement(t.statementId));
            }
            float height = Mathf.Max(900, (Mathf.Ceil(theories.Count / 5f) + 2) * 192, clues.Count * 88, (statements.Count + 3) * 88);
            model.size = new Vector2(1160, height);
            for (int i = 0; i < clues.Count; i++)
            {
                var clue = clues[i]; bool anticipated = !progress.clues.Contains(clue.id);
                var hint = progress.clueHints.Find(h => h.clueId == clue.id);
                model.nodes.Add(new ReasoningNode { id = clue.id, kind = ReasoningNodeKind.Clue, anticipated = anticipated,
                    label = clue.name + (anticipated ? " (not yet found)" : ""),
                    detail = anticipated ? "CLUE HINT — not collected\n" + clue.hint + "\nMentioned by " + hint?.speaker + ". Approach its marker in the environment and inspect it." : clue.fact,
                    position = new Vector2(110, 80 + i * (height - 160) / Mathf.Max(1, clues.Count - 1)) });
            }
            for (int i = 0; i < statements.Count; i++) model.nodes.Add(new ReasoningNode { id = statements[i].id, kind = ReasoningNodeKind.Statement, speaker = statements[i].speaker, label = statements[i].speaker + " " + statements[i].id.Split('-').Last(), detail = statements[i].claim, position = new Vector2(1020, 80 + i * (height - 160) / Mathf.Max(1, statements.Count - 1)) });
            float Barycentre(TheoryDefinition t)
            {
                var a = model.nodes.Find(n => n.id == t.clueId); var b = model.nodes.Find(n => n.id == t.statementId);
                return ((a?.position.y ?? 0) + (b?.position.y ?? 0)) * .5f;
            }
            theories = theories.OrderBy(Barycentre).ThenBy(t => t.id, StringComparer.Ordinal).ToList();
            var placed = new List<Vector2>();
            for (int i = 0; i < theories.Count; i++)
            {
                var t = theories[i];
                // Intersect the straight parent connection with the centre of the theories band.
                const float columnX = 565;
                var clue = model.nodes.Find(n => n.id == t.clueId);
                var statement = model.nodes.Find(n => n.id == t.statementId);
                float fraction = (columnX - clue.position.x) / (statement.position.x - clue.position.x);
                float desiredY = Mathf.Lerp(clue.position.y, statement.position.y, fraction);
                var candidates = new List<Vector2>();
                // Find the closest point in each genuinely free vertical interval, rather
                // than assuming every desired position shares the same fixed row grid.
                while (candidates.Count == 0)
                {
                    foreach (float dx in new[] { 0f, -80f, 80f, -160f, 160f })
                    {
                        float x = columnX + dx, start = 80;
                        foreach (var occupied in placed.Where(p => Mathf.Abs(p.x - x) < 79.9f).OrderBy(p => p.y))
                        {
                            float end = occupied.y - 96;
                            if (end >= start) candidates.Add(new Vector2(x, Mathf.Clamp(desiredY, start, end)));
                            start = Mathf.Max(start, occupied.y + 96);
                        }
                        if (start <= height - 80) candidates.Add(new Vector2(x, Mathf.Clamp(desiredY, start, height - 80)));
                    }
                    if (candidates.Count == 0) { height += 192; model.size = new Vector2(model.size.x, height); }
                }
                Vector2 ideal = new Vector2(columnX, desiredY);
                Vector2 position = candidates.OrderBy(p => (p - ideal).sqrMagnitude).ThenBy(p => Mathf.Abs(p.x - columnX)).ThenBy(p => p.y).ThenBy(p => p.x)
                    .First(p => placed.All(other => Mathf.Abs(p.x - other.x) >= 79.9f || Mathf.Abs(p.y - other.y) >= 95.9f));
                placed.Add(position);
                model.nodes.Add(new ReasoningNode { id = t.id, kind = ReasoningNodeKind.Theory, label = QuestionLabel(progress, t.id), detail = t.text,
                    speaker = catalogue.Statement(t.statementId).speaker, clueParent = t.clueId, statementParent = t.statementId,
                    position = position });
            }
            return model;
        }
    }
}
