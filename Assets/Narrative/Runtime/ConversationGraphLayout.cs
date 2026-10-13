using System.Linq;

namespace GraduationDemo.Narrative
{
    public static class ConversationGraphLayout
    {
        public static ReasoningGraphLayout Build(InvestigationState state, string suspect, string search = "")
        {
            var progress = new InvestigationProgress();
            foreach (var prompt in state.AvailableConversations(suspect).Where(p => p.kind == ConversationKind.Theory))
            {
                var theory = state.catalogue.Theory(prompt.theoryId);
                progress.theories.Add(theory.id);
                if (!progress.clues.Contains(theory.clueId)) progress.clues.Add(theory.clueId);
                if (!progress.statements.Contains(theory.statementId)) progress.statements.Add(theory.statementId);
            }
            return ReasoningGraphLayout.Build(state.catalogue, progress, search);
        }
    }
}
