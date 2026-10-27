using System.Collections.Generic;
using System.Linq;

namespace GraduationDemo.Narrative
{
    public static class NarrativeValidation
    {
        public static List<string> Errors(NarrativeCatalogue c)
        {
            var errors = new List<string>();
            Duplicate(c.clues.Select(x => x.id), "clue", errors);
            Duplicate(c.statements.Select(x => x.id), "statement", errors);
            Duplicate(c.theories.Select(x => x.id), "question", errors);
            Duplicate(c.voices.Select(x => x.id), "voice", errors);
            Duplicate(c.sequences.Select(x => x.id), "sequence", errors);
            Duplicate(c.routes.Select(x => x.id), "route", errors);
            Duplicate(c.theories.Select(x => x.clueId + "/" + x.statementId), "pair", errors);
            foreach (var t in c.theories)
                if (c.Clue(t.clueId) == null || c.Statement(t.statementId) == null || string.IsNullOrWhiteSpace(t.text)) errors.Add("Invalid question " + t.id);
            foreach (var clue in c.clues)
                foreach (var statement in c.statements)
                    if (c.Pair(clue.id, statement.id) == null) errors.Add("Missing pair " + clue.id + "/" + statement.id);
            foreach (var v in c.voices)
                if (string.IsNullOrWhiteSpace(v.subtitle) || !new[] { "Burrows", "Felix", "Mila", "Bellamy", "Odette" }.Contains(v.speaker)) errors.Add("Invalid voice " + v.id);
            foreach (var s in c.sequences)
            {
                if (s.beats.Count == 0) errors.Add("Empty sequence " + s.id);
                foreach (var b in s.beats)
                {
                    if (c.Voice(b.voiceId) == null) errors.Add("Missing voice " + b.voiceId + " in " + s.id);
                    foreach (var id in b.grants) Reward(c, id, s.id, errors);
                    foreach (var id in b.clueHints) if (c.Clue(id) == null) errors.Add("Unknown clue hint " + id + " in " + s.id);
                }
            }
            foreach (var clue in c.clues.Where(x => x.hiddenUntilHint))
            {
                if (string.IsNullOrWhiteSpace(clue.hint)) errors.Add("Hidden clue needs search context: " + clue.id);
                if (!c.sequences.SelectMany(s => s.beats).Any(b => c.ClueHints(b).Contains(clue.id))) errors.Add("Hidden clue has no dialogue hint: " + clue.id);
            }
            foreach (var r in c.routes)
            {
                if (c.Theory(r.theoryId) == null || !NarrativeCatalogue.IsSuspect(r.suspect)) errors.Add("Invalid route target " + r.id);
                if (c.Sequence(r.sequenceId) == null || c.Sequence(r.repeatSequenceId) == null) errors.Add("Missing route sequence " + r.id);
                foreach (var id in r.grants.Concat(r.required).Concat(r.forbidden)) Reward(c, id, r.id, errors);
                if (r.required.Intersect(r.forbidden).Any()) errors.Add("Unsatisfiable conditions " + r.id);
                var sourceTheory = c.Theory(r.theoryId);
                if (sourceTheory != null && r.forbidden.Any(id => id == sourceTheory.clueId || id == sourceTheory.statementId)) errors.Add("Route forbids its own ingredient " + r.id);
                if (c.Sequence(r.sequenceId)?.beats.Any(b => c.Voice(b.voiceId)?.disclosesFallGeometry == true) == true &&
                    sourceTheory?.statementId != "M-S05" && !r.required.Contains("M-S05")) errors.Add("Premature confidential geometry disclosure " + r.id);
                if (r.grants.Any(id => c.Statement(id) != null) && c.Sequence(r.sequenceId) != null &&
                    r.grants.Any(id => !c.Sequence(r.sequenceId).beats.Any(b => b.grants.Contains(id))))
                    errors.Add("Route reward lacks spoken acquisition beat " + r.id);
            }
            // Fixed-point reachability from physical discoveries and spoken initial interviews.
            var reachable = new HashSet<string>(c.clues.Where(x => !x.hiddenUntilHint).Select(x => x.id).Concat(c.statements.Where(x => x.initial).Select(x => x.id)));
            foreach (var initial in c.sequences.Where(s => s.id.StartsWith("OPENING") || s.id.Contains("alibi") || s.id.Contains("rehearsal") || s.id.Contains("Corridor witness")))
                foreach (var id in initial.beats.SelectMany(c.ClueHints)) reachable.Add(id);
            reachable.Add("flag:investigation");
            bool changed;
            do
            {
                changed = false;
                foreach (var r in c.routes)
                {
                    var t = c.Theory(r.theoryId);
                    if (t == null || !reachable.Contains(t.clueId) || !reachable.Contains(t.statementId) || r.required.Any(x => !reachable.Contains(x))) continue;
                    foreach (var id in r.grants) if (reachable.Add(id)) changed = true;
                    foreach (var id in c.Sequence(r.sequenceId)?.beats.SelectMany(c.ClueHints) ?? Enumerable.Empty<string>())
                        if (reachable.Add(id)) changed = true;
                }
            } while (changed);
            foreach (var clue in c.clues) if (!reachable.Contains(clue.id)) errors.Add("Unreachable clue hint / circular dependency " + clue.id);
            foreach (var s in c.statements) if (!reachable.Contains(s.id)) errors.Add("Unreachable statement / circular dependency " + s.id);
            foreach (var s in c.sequences.Where(s => s.id.StartsWith("OPENING") || s.id.Contains("alibi") || s.id.Contains("rehearsal") || s.id.Contains("Corridor witness")))
                if (s.beats.Any(b => c.Voice(b.voiceId)?.disclosesFallGeometry == true)) errors.Add("Confidential geometry in opening / initial interview " + s.id);
            foreach (var t in c.theories)
                foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" })
                    if (!c.routes.Any(r => r.theoryId == t.id && r.suspect == suspect)) errors.Add("Missing suspect route " + t.id + "/" + suspect);
            foreach (var a in c.arrestRules.Where(x => x.enabled))
            {
                if (!NarrativeCatalogue.IsSuspect(a.suspect) || c.Sequence(a.endingSequence) == null) errors.Add("Invalid arrest rule " + a.id);
                foreach (var id in a.requiredStatements) if (c.Statement(id) == null) errors.Add("Missing arrest statement " + id);
                foreach (var id in a.requiredClues) if (c.Clue(id) == null) errors.Add("Missing arrest clue " + id);
                foreach (var p in a.propositions) if (!c.theories.Any(t => t.propositions.Contains(p))) errors.Add("Unsupported arrest proposition " + p);
            }
            return errors;
        }
        static void Reward(NarrativeCatalogue c, string id, string source, List<string> errors)
        {
            if (c.Clue(id) == null && c.Statement(id) == null && !id.StartsWith("flag:")) errors.Add("Unknown reference " + id + " in " + source);
        }
        static void Duplicate(IEnumerable<string> ids, string type, List<string> errors)
        {
            foreach (var group in ids.GroupBy(x => x)) if (string.IsNullOrEmpty(group.Key) || group.Count() > 1) errors.Add("Empty / duplicate " + type + " ID " + group.Key);
        }
    }
}
