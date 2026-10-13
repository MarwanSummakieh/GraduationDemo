using System.Collections.Generic;
using System.Linq;

namespace GraduationDemo.Narrative
{
    public sealed class AccusationCheck
    {
        public string key, label, explanation;
        public bool satisfied, criterion;
    }
    public sealed class AccusationAssessment
    {
        public ArrestRule rule;
        public readonly List<AccusationCheck> checks = new List<AccusationCheck>();
        public readonly List<string> notes = new List<string>();
        public bool Supported => rule != null && checks.All(c => c.satisfied);
        public string DisplayText => "CASE SUPPORT: " + (Supported ? "SUPPORTED" : "INCOMPLETE") + "\n" +
            string.Join("\n", checks.Where(c => c.criterion).Select(c => (c.satisfied ? "[OK] " : "[Missing] ") + c.label)) +
            string.Concat(checks.Where(c => !c.satisfied && !c.criterion).Select(c => "\n[Missing] " + c.explanation)) +
            (notes.Count == 0 ? "" : "\n\n" + string.Join("\n", notes));
        public string Reason => Supported ? "Supported accusation: " + rule.id :
            "Provisional unresolved case: " + string.Join(" ", checks.Where(c => !c.satisfied).Select(c => c.explanation));
        public void Add(string key, string label, bool satisfied, string explanation, bool criterion = false) =>
            checks.Add(new AccusationCheck { key = key, label = label, satisfied = satisfied, explanation = explanation, criterion = criterion });
    }
}
