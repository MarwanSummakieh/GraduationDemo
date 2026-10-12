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
        [Min(1)] public int actionsPerDay = 3;
        [Min(0)] public int alibiCost;
        [Min(0)] public int confrontationCost = 1;
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
            if (investigationDays < 1 || actionsPerDay < 1 || alibiCost < 0 || confrontationCost < 0) errors.Add("Days/actions must be positive; costs cannot be negative.");
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
        public RouteSelection selection;
    }
    [Serializable] public class ConversationReservation
    {
        public string key, sequenceId;
        public string sequenceKey;
        public int cost, day;
        public List<string> grantedDuringPlayback = new List<string>();
    }
}
