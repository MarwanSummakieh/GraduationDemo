using System;
using System.Collections.Generic;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    public enum InvestigationPhase { Investigating, Accusation, Resolved }
    public enum ConversationKind { Alibi, Theory }
    public enum ReasoningAssistance { Off, Subtle, Guided, Directed }
    public enum ReasoningHintKind { Contradiction, Ambiguity, CaseConnection }
    public enum ReasoningHintPurpose { Interview, CaseReview }
    [Serializable] public class ReasoningHint
    {
        public string id, firstId, secondId;
        public ReasoningHintKind kind;
        public ReasoningHintPurpose purpose;
        public string recipient;
        [Tooltip("Hide this lead once any listed discovery has been learned, including through an alternative route.")]
        public List<string> resolvedBy = new List<string>();
        [TextArea] public string prompt;
    }
    [Serializable] public class JourneyStop { public string name; public Vector2 position; }
    [CreateAssetMenu(menuName = "Murder Most Inconvenient/Investigation pacing")]
    public sealed class InvestigationConfiguration : ScriptableObject
    {
        [Min(1)] public int investigationDays = 3;
        [Tooltip("Enable daily action spending. Disabled for unrestricted playtesting.")] public bool actionCostsEnabled = false;
        [Min(1)] public int actionsPerDay = 3;
        [Min(0)] public int alibiCost;
        [InspectorName("Question Cost"), Min(0), Tooltip("Actions spent when beginning a conversation about a constructed question, if action costs are enabled.")]
        public int confrontationCost = 1;
        [Min(0), Tooltip("Minimum conversation cost after an incorrect intuition. Charged once per conversation when action costs are enabled.")]
        public int intuitionMistakeCost = 1;
        public DoubtTuning doubtTuning = new DoubtTuning();
        [Header("Defense presentation")]
        [Range(0, 1)] public float defenseSoundVolume = .35f;
        [InspectorName("Reduced Presentation Motion"), Tooltip("Reduce decorative neural movement, portrait animation and defense shake.")] public bool defenseReducedMotion;
        [Min(0)] public int coffeeFocusReward = 2;
        [Min(0)] public int intuitionFocusReward = 1;
        [HideInInspector] public List<DoubtDefinition> doubts = DoubtRules.Defaults();
        public List<AuthoredDoubtLink> doubtLinks = DoubtRules.AuthoredLinks();
        public ReasoningAssistance reasoningAssistance = ReasoningAssistance.Subtle;
        public List<ReasoningHint> reasoningHints = ReasoningGuidance.DefaultHints();
        public List<JourneyStop> stops = new List<JourneyStop> {
            new JourneyStop { name = "Departure", position = new Vector2(.12f, .28f) },
            new JourneyStop { name = "Valley stop", position = new Vector2(.36f, .48f) },
            new JourneyStop { name = "Border stop", position = new Vector2(.62f, .40f) },
            new JourneyStop { name = "Destination", position = new Vector2(.86f, .72f) }
        };
        public List<string> Errors()
        {
            var errors = new List<string>();
            if (coffeeFocusReward < 0 || intuitionFocusReward < 0) errors.Add("Focus rewards cannot be negative.");
            if (float.IsNaN(defenseSoundVolume) || defenseSoundVolume < 0 || defenseSoundVolume > 1) errors.Add("Defense sound volume must be between zero and one.");
            if (doubtLinks == null || doubtLinks.Exists(d => d == null || string.IsNullOrEmpty(d.id) || string.IsNullOrEmpty(d.questionId) || string.IsNullOrEmpty(d.sourceId) || string.IsNullOrWhiteSpace(d.reason) || d.weight < 1)) errors.Add("Authored doubt links need IDs, question, source, reason and positive weight.");
            if (doubtLinks != null && new HashSet<string>(doubtLinks.ConvertAll(d => d?.id)).Count != doubtLinks.Count) errors.Add("Duplicate authored doubt link ID.");
            if (doubtLinks != null && doubtLinks.Exists(d => d != null && d.questionId == d.sourceId)) errors.Add("A doubt must connect a question to another node.");
            if (doubtTuning == null) errors.Add("Doubt tuning is required."); else errors.AddRange(doubtTuning.Errors());
            if (doubts == null || doubts.Exists(d => d == null || string.IsNullOrEmpty(d.id) || string.IsNullOrEmpty(d.clueId) || string.IsNullOrEmpty(d.statementId) || d.challenges == null || d.challenges.Count == 0)) errors.Add("Doubts need IDs, source entries and at least one challenge.");
            if (doubts != null)
            {
                var ids = new HashSet<string>();
                foreach (var d in doubts)
                {
                    if (d == null) continue;
                    if (!ids.Add(d.id)) errors.Add("Duplicate doubt ID: " + d.id);
                    if (d.role != AccusationReasoning.Motive && d.role != AccusationReasoning.Opportunity) errors.Add("Doubt needs a Motive or Opportunity review role: " + d.id);
                    if (string.IsNullOrWhiteSpace(d.title) || string.IsNullOrWhiteSpace(d.explanation) || string.IsNullOrWhiteSpace(d.interpretation) || d.challenges != null && d.challenges.Exists(string.IsNullOrWhiteSpace)) errors.Add("Doubt needs readable interpretation, explanation and challenge text: " + d.id);
                }
            }
            if (investigationDays < 1 || actionsPerDay < 1 || alibiCost < 0 || confrontationCost < 0 || intuitionMistakeCost < 0) errors.Add("Days/actions must be positive; costs cannot be negative.");
            if (stops == null || stops.Count < 2) errors.Add("The journey needs at least two stops.");
            else foreach (var stop in stops) if (stop == null || string.IsNullOrWhiteSpace(stop.name)) errors.Add("Every journey stop needs a readable name.");
            return errors;
        }
    }
    [Serializable] public class DiscoveryProvenance { public string id, locationId; public int day; }
    public sealed class ConversationInitialization
    {
        public string key, suspect, theoryId, sequenceId;
        public ConversationKind kind;
        public int cost;
        public bool blockedByDoubt;
        public RouteSelection selection;
    }
    [Serializable] public class ConversationReservation
    {
        public string key, sequenceId;
        public string sequenceKey;
        public int cost, day;
        public int originalCost;
        public bool intuitionMistake;
        public List<string> interpretedEntries = new List<string>();
        public List<string> grantedDuringPlayback = new List<string>();
    }
}
