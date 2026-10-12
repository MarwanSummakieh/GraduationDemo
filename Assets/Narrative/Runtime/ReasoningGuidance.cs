using System.Collections.Generic;

namespace GraduationDemo.Narrative
{
    public static class ReasoningGuidance
    {
        public static List<ReasoningHint> Available(InvestigationConfiguration configuration, InvestigationProgress progress, ReasoningAssistance assistance)
        {
            var result = new List<ReasoningHint>();
            if (configuration == null || progress == null || assistance == ReasoningAssistance.Off || configuration.reasoningHints == null) return result;
            foreach (var hint in configuration.reasoningHints)
            {
                if (hint == null || string.IsNullOrEmpty(hint.id) || string.IsNullOrEmpty(hint.prompt) || hint.firstId == hint.secondId) continue;
                if (Discovered(progress, hint.firstId) && Discovered(progress, hint.secondId)) result.Add(hint);
            }
            return result;
        }
        static bool Discovered(InvestigationProgress progress, string id) => !string.IsNullOrEmpty(id) &&
            (progress.clues.Contains(id) || progress.statements.Contains(id) || progress.theories.Contains(id));
        public static string Text(ReasoningHint hint, ReasoningAssistance assistance)
        {
            string label = hint.kind == ReasoningHintKind.Contradiction ? "Potential contradiction" : "Relevant ambiguity";
            return assistance == ReasoningAssistance.Guided
                ? label + " / " + hint.firstId + " ↔ " + hint.secondId + "\n" + hint.prompt
                : label;
        }
    }
}
