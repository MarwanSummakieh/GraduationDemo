using System.Collections.Generic;
using System.Linq;

namespace GraduationDemo.Narrative
{
    public static class ReasoningGuidance
    {
        // Recipient metadata is exposed only in Directed mode; prose never reveals future rewards.
        public static List<ReasoningHint> DefaultHints() => new List<ReasoningHint> {
            Lead("engagement_context", "C06", "F-S02", "F-S03", "Matching rings suggest a private commitment. Could the champagne paid for by Odette have been a celebration rather than an ordinary drink?"),
            Lead("rehearsal_bar_context", "C09", "F-S01", "F-S04", "The rehearsal ban explains why guests stayed away. Could the account of time at the bar help establish what was happening outside the private rehearsal?"),
            Lead("preparations_bar_context", "C08", "F-S01", "F-S05", "The syrup was part of the masquerade preparations; the bar account concerns a different activity. Could preparations or exchanges of costume items connect those activities?"),
            Lead("privacy_and_authority", "C09", "B-S02", "B-S03", "A strict privacy rule kept people away from the rehearsal. Was that rule accepted without disagreement, or might it have caused friction?"),
            Lead("clock_and_private_rehearsal", "C02", "B-S02", "B-S04", "The clock stopped at about 20:37 during a private rehearsal. Could a disturbance have been mistaken for part of the performance rather than checked?"),
            Lead("syrup_and_crash", "C08", "B-S01", "B-S05", "Stage blood preparations offer a theatrical explanation for some of the scene. Could the reported sharp noise and crash have involved rehearsal materials?"),
            Lead("rings_and_happy_mood", "C06", "M-S03", "M-S06", "The matching rings suggest news beyond the masquerade. Could that explain the reported excitement, and what would knowing its cause imply?"),
            Lead("clock_and_happy_mood", "C02", "M-S03", "M-S04", "A happy mood does not establish when or how the clock broke. What explanation for the damaged clock follows from this account, if any?"),
            Lead("desk_and_clumsiness", "C07", "M-S04", "M-S05", "Could ordinary clumsiness account for the displaced furniture and impact damage? Compare the physical desk evidence with the suggested accident."),
            Lead("clock_position_and_clumsiness", "C10", "M-S04", "M-S05", "Could a casually knocked-over clock end up behind the desk? Compare where it was found with the suggested accident."),
            Case("injury_and_crash", "C03", "B-S01", "Injury: compare the observed head/neck trauma with the reported crash. This helps examine whether the real injury fits the fictional stabbing."),
            Case("clock_and_crash", "C02", "B-S01", "Injury: compare the stopped heavy clock with the reported crash. The time and sound need independent physical corroboration."),
            Case("rings_and_engagement", "C06", "F-S03", "Context: the admission of an engagement explains the matching rings. A private relationship alone does not establish responsibility for the death."),
            Case("presence_despite_privacy", "C09", "F-S04", "Presence: compare the prohibition on interruptions with the sighting near the private rehearsal. This can support your final case; it does not open another interview."),
            Case("programme_and_sighting", "C04", "F-S04", "Presence: compare the sighting around 20:45 with the fictional murder scheduled for 22:00. Keep the sighting separate from proof of the injury."),
            Case("costume_preparations", "C08", "F-S05", "Context: costume preparation and the borrowed safety pin belong to the masquerade. Neither detail alone establishes when someone entered Odette's compartment."),
            Case("privacy_and_argument", "C09", "B-S03", "Context: compare the admitted argument with the privacy rule. Friction provides a question about relationships, not proof of guilt."),
            Case("clock_and_assumption", "C02", "B-S04", "Context: the clock's damage is physical evidence; calling a crash part of the performance was an assumption. Keep observation and interpretation separate."),
            Case("syrup_preparation_account", "C08", "B-S05", "Context: compare the account of preparing theatrical blood with the syrup materials. Participation in preparations does not establish responsibility for the real injury."),
            Case("rings_and_reaction", "C06", "M-S06", "Context: compare the claimed happiness for the couple with the matching rings. A reported emotion is not independent evidence of innocence or guilt."),
            Case("clock_and_speculation", "C02", "M-S04", "Context: the suggestion of clumsiness is an explanation offered for the broken clock, not an observed cause. Investigate the physical desk or clock position to examine that suggestion."),
            Case("desk_and_spatial_account", "C07", "M-S05", "Contradiction: compare the damaged desk with the claim about what Odette could reach from where she fell. Consider whether the speaker could know that geometry, and whether Burrows had disclosed it."),
            Case("clock_and_spatial_account", "C10", "M-S05", "Contradiction: compare the clock's concealed position with the precise claim about what Odette could reach. Examine the source of that knowledge; an evasive tone alone is not evidence.")
        };
        static ReasoningHint Lead(string id, string clue, string statement, string reward, string prompt) =>
            new ReasoningHint { id = id, firstId = clue, secondId = statement, kind = ReasoningHintKind.Ambiguity, prompt = prompt,
                recipient = statement.StartsWith("F-") ? "Felix" : statement.StartsWith("B-") ? "Bellamy" : "Mila", resolvedBy = new List<string> { reward } };
        static ReasoningHint Case(string id, string clue, string statement, string prompt) =>
            new ReasoningHint { id = id, firstId = clue, secondId = statement, purpose = ReasoningHintPurpose.CaseReview, kind = ReasoningHintKind.CaseConnection, prompt = prompt };
        public static bool ShowsConnections(ReasoningAssistance assistance) => assistance == ReasoningAssistance.Guided || assistance == ReasoningAssistance.Directed;
        public static bool Matches(ReasoningHint hint, TheoryDefinition theory) => theory != null &&
            (hint.firstId == theory.id || hint.secondId == theory.id || hint.firstId == theory.clueId && hint.secondId == theory.statementId || hint.secondId == theory.clueId && hint.firstId == theory.statementId);
        public static string InterviewNote(NarrativeCatalogue catalogue, InvestigationProgress progress, ConversationInitialization conversation)
        {
            if (conversation == null || conversation.kind != ConversationKind.Theory) return "";
            var sequence = catalogue.Sequence(conversation.sequenceId);
            bool newDiscovery = (sequence?.beats.SelectMany(b => b.grants) ?? Enumerable.Empty<string>())
                .Concat(conversation.selection?.route?.grants ?? new List<string>())
                .Any(id => (catalogue.Statement(id) != null || catalogue.Clue(id) != null) && !Discovered(progress, id));
            return newDiscovery ? "This interview can develop a new line of inquiry." :
                "No new testimony is expected from this pairing with " + conversation.suspect + ". You can still hear their reaction, or cancel and review the theory for your final case.";
        }
        public static List<ReasoningHint> Available(InvestigationConfiguration configuration, InvestigationProgress progress, ReasoningAssistance assistance)
        {
            var result = new List<ReasoningHint>();
            if (configuration == null || progress == null || assistance == ReasoningAssistance.Off || configuration.reasoningHints == null) return result;
            foreach (var hint in configuration.reasoningHints)
            {
                if (hint == null || string.IsNullOrEmpty(hint.id) || string.IsNullOrEmpty(hint.prompt) || hint.firstId == hint.secondId) continue;
                if (hint.purpose == ReasoningHintPurpose.Interview && progress.phase != InvestigationPhase.Investigating) continue;
                if (hint.resolvedBy != null && hint.resolvedBy.Any(id => Discovered(progress, id))) continue;
                if (Discovered(progress, hint.firstId) && Discovered(progress, hint.secondId)) result.Add(hint);
            }
            return result;
        }
        static bool Discovered(InvestigationProgress progress, string id) => !string.IsNullOrEmpty(id) &&
            (progress.clues.Contains(id) || progress.statements.Contains(id) || progress.theories.Contains(id));
        public static string Text(ReasoningHint hint, ReasoningAssistance assistance)
        {
            string label = hint.kind == ReasoningHintKind.Contradiction ? "Potential contradiction" : hint.kind == ReasoningHintKind.CaseConnection ? "Case-building connection" : "Interview lead";
            if (!ShowsConnections(assistance)) return label;
            string text = label + " / " + hint.firstId + " ↔ " + hint.secondId + "\n" + hint.prompt;
            if (hint.purpose == ReasoningHintPurpose.CaseReview) text += "\nReview for your final case; no further interview is suggested.";
            else if (assistance == ReasoningAssistance.Directed && NarrativeCatalogue.IsSuspect(hint.recipient)) text += "\nConstruct this theory, then bring it to " + hint.recipient + ".";
            return text;
        }
    }
}
