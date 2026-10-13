using System;
using System.Collections.Generic;
using System.Linq;

namespace GraduationDemo.Narrative
{
    [Serializable] public sealed class InvestigationProgress
    {
        public List<string> clues = new List<string>(), statements = new List<string>(), theories = new List<string>();
        public List<string> flags = new List<string>(), completedRoutes = new List<string>();
        public List<string> usedInitializations = new List<string>();
        public List<string> removedTheories = new List<string>();
        public List<string> usedConversationSequences = new List<string>();
        public List<DiscoveryProvenance> discoveries = new List<DiscoveryProvenance>();
        public int day = 1, actionsSpent;
        public InvestigationPhase phase;
        public ConversationReservation reservation;
    }
    public sealed class RouteSelection
    {
        public RouteDefinition route;
        public DialogueDefinition sequence;
        public bool repeat;
        public string explanation;
    }
    /// <summary>Pure deduction rules; definitions never contain mutable player discoveries.</summary>
    public sealed class InvestigationState
    {
        public readonly NarrativeCatalogue catalogue;
        public readonly InvestigationProgress progress;
        public readonly InvestigationConfiguration configuration;
        public event Action Changed;
        public InvestigationState(NarrativeCatalogue data, InvestigationProgress restored = null, InvestigationConfiguration pacing = null)
        {
            catalogue = data ?? throw new ArgumentNullException(nameof(data));
            progress = restored ?? new InvestigationProgress();
            configuration = pacing;
            foreach (var id in progress.clues.Concat(progress.statements))
                if (!progress.discoveries.Any(x => x.id == id)) progress.discoveries.Add(new DiscoveryProvenance { id = id, locationId = "unknown", day = 0 });
        }
        public int Days => configuration == null ? 3 : configuration.investigationDays;
        public int ActionsPerDay => configuration == null ? 3 : configuration.actionsPerDay;
        public int RemainingActions => Math.Max(0, ActionsPerDay - progress.actionsSpent);
        public bool InvestigationOpen => progress.phase == InvestigationPhase.Investigating;
        public bool CanConstructTheories => progress.phase == InvestigationPhase.Investigating || progress.phase == InvestigationPhase.Accusation;
        public bool Has(string id) => progress.clues.Contains(id) || progress.statements.Contains(id) || progress.flags.Contains(id);
        public bool Grant(string id) => Grant(id, "unknown");
        public bool Grant(string id, string locationId)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            bool discovery = catalogue.Clue(id) != null || catalogue.Statement(id) != null;
            if (discovery && !InvestigationOpen) return false;
            bool added;
            if (catalogue.Clue(id) != null) added = Add(progress.clues, id);
            else if (catalogue.Statement(id) != null) added = Add(progress.statements, id);
            else if (id.StartsWith("flag:")) added = Add(progress.flags, id);
            else throw new ArgumentException("Unknown narrative reward: " + id);
            if (added)
            {
                if (discovery) progress.discoveries.Add(new DiscoveryProvenance { id = id, locationId = string.IsNullOrEmpty(locationId) ? "unknown" : locationId, day = progress.day });
                if (progress.reservation != null) progress.reservation.grantedDuringPlayback.Add(id);
                if (id == "flag:resolved") progress.phase = InvestigationPhase.Resolved;
                Changed?.Invoke();
            }
            return added;
        }
        static bool Add(List<string> values, string id) { if (values.Contains(id)) return false; values.Add(id); return true; }
        public TheoryDefinition Construct(string first, string second)
        {
            if (!CanConstructTheories) return null;
            string clue = catalogue.Clue(first) != null ? first : second;
            string statement = catalogue.Statement(second) != null ? second : first;
            if (!progress.clues.Contains(clue) || !progress.statements.Contains(statement)) return null;
            var theory = catalogue.Pair(clue, statement);
            if (theory != null && Add(progress.theories, theory.id)) { progress.removedTheories.Remove(theory.id); Changed?.Invoke(); }
            return theory;
        }
        public bool RemoveTheory(string id)
        {
            if (progress.reservation != null || !progress.theories.Remove(id)) return false;
            Add(progress.removedTheories, id);
            // Removing notebook content never erases evidence, testimony or consumed conversations.
            Changed?.Invoke(); return true;
        }
        public bool RestoreTheory(string id)
        {
            var theory = catalogue.Theory(id);
            if (progress.reservation != null || theory == null || !progress.removedTheories.Contains(id) || !progress.clues.Contains(theory.clueId) || !progress.statements.Contains(theory.statementId)) return false;
            progress.removedTheories.Remove(id); Add(progress.theories, id); Changed?.Invoke(); return true;
        }
        public RouteSelection SelectRoute(string theoryId, string suspect)
        {
            if (!InvestigationOpen || !progress.theories.Contains(theoryId) || !NarrativeCatalogue.IsSuspect(suspect)) return null;
            string prefix = "theory/" + theoryId + "/" + suspect + "/";
            bool presented = progress.usedInitializations.Any(x => x.StartsWith(prefix, StringComparison.Ordinal));
            var candidates = catalogue.routes.Where(x => x.theoryId == theoryId && x.suspect == suspect)
                .OrderByDescending(x => x.priority).ThenBy(x => x.id, StringComparer.Ordinal).ToList();
            var reasons = new List<string>();
            foreach (var route in candidates)
            {
                bool completed = progress.completedRoutes.Contains(route.id) || progress.usedInitializations.Contains(prefix + route.id);
                bool revelationKnown = route.grants.Count > 0 && route.grants.All(Has);
                if (completed || revelationKnown)
                {
                    reasons.Add(route.id + ": revelation already disclosed / encounter completed");
                    continue;
                }
                if (presented && route.priority <= 0) { reasons.Add(route.id + ": presentation already used; no newly eligible authored response"); continue; }
                if (presented && progress.usedConversationSequences.Contains(prefix + route.sequenceId)) { reasons.Add(route.id + ": same dialogue sequence already used for this presentation"); continue; }
                var missing = route.required.Where(x => !Has(x)).ToArray();
                var forbidden = route.forbidden.Where(Has).ToArray();
                if (missing.Length > 0 || forbidden.Length > 0)
                {
                    reasons.Add(route.id + ": missing " + string.Join(",", missing) + "; already present " + string.Join(",", forbidden));
                    continue;
                }
                var sequence = catalogue.Sequence(route.sequenceId);
                if (sequence == null) { reasons.Add(route.id + ": missing sequence"); continue; }
                return Selection(route, sequence, false, reasons);
            }
            if (!presented && candidates.Count == 0) return Selection(null, catalogue.Sequence("Reusable/" + suspect), false, reasons);
            return null;
        }
        static RouteSelection Selection(RouteDefinition route, DialogueDefinition sequence, bool repeat, List<string> reasons)
        {
            return new RouteSelection { route = route, sequence = sequence, repeat = repeat,
                explanation = string.Join("\n", reasons) + "\nSelected " + (route == null ? "fallback" : route.id) + "; sequence " + (sequence == null ? "MISSING" : sequence.id) };
        }
        public void Complete(RouteSelection selection)
        {
            if (selection == null || selection.route == null || selection.repeat) return;
            foreach (var id in selection.route.grants) Grant(id);
            Add(progress.completedRoutes, selection.route.id);
        }
        public ConversationInitialization Alibi(string suspect)
        {
            string sequence = suspect == "Felix" ? "Felix — Champagne alibi" : suspect == "Mila" ? "Mila — Solitary rehearsal" : "Bellamy — Corridor witness";
            string key = "alibi/" + suspect;
            if (!InvestigationOpen || !NarrativeCatalogue.IsSuspect(suspect) || progress.usedInitializations.Contains(key) || catalogue.Sequence(sequence) == null) return null;
            return new ConversationInitialization { key = key, suspect = suspect, sequenceId = sequence, kind = ConversationKind.Alibi, cost = configuration == null ? 0 : configuration.alibiCost };
        }
        public ConversationInitialization Presentation(string theory, string suspect)
        {
            var selection = SelectRoute(theory, suspect);
            if (selection?.sequence == null) return null;
            return new ConversationInitialization { key = "theory/" + theory + "/" + suspect + "/" + (selection.route?.id ?? "fallback"), suspect = suspect, theoryId = theory,
                sequenceId = selection.sequence.id, selection = selection, kind = ConversationKind.Theory, cost = configuration == null ? 1 : configuration.confrontationCost };
        }
        public List<ConversationInitialization> AvailableConversations(string suspect)
        {
            var values = new List<ConversationInitialization>();
            var alibi = Alibi(suspect); if (alibi != null) values.Add(alibi);
            foreach (var theory in progress.theories) { var prompt = Presentation(theory, suspect); if (prompt != null) values.Add(prompt); }
            return values;
        }
        public bool TryInitialize(ConversationInitialization offered, out string reason)
        {
            if (offered == null || progress.reservation != null || !InvestigationOpen) { reason = "No conversation can start now."; return false; }
            var current = offered.kind == ConversationKind.Alibi ? Alibi(offered.suspect) : Presentation(offered.theoryId, offered.suspect);
            if (current == null || current.key != offered.key || current.sequenceId != offered.sequenceId || current.cost != offered.cost)
            { reason = "This conversation prompt is no longer available."; return false; }
            if (current.cost > RemainingActions) { reason = "Not enough actions remain today."; return false; }
            progress.actionsSpent += current.cost; progress.usedInitializations.Add(current.key);
            string sequenceKey = current.kind == ConversationKind.Theory ? "theory/" + current.theoryId + "/" + current.suspect + "/" + current.sequenceId : current.key + "/" + current.sequenceId;
            Add(progress.usedConversationSequences, sequenceKey);
            progress.reservation = new ConversationReservation { key = current.key, sequenceId = current.sequenceId, sequenceKey = sequenceKey, cost = current.cost, day = progress.day };
            reason = "Conversation started."; Changed?.Invoke(); return true;
        }
        public void FinishConversation(RouteSelection selection)
        {
            Complete(selection); progress.reservation = null; Changed?.Invoke();
        }
        public void RecoverFailedConversation()
        {
            var reservation = progress.reservation; if (reservation == null) return;
            // Retry remains possible; testimony already heard is kept and never awarded twice.
            progress.usedInitializations.Remove(reservation.key);
            progress.usedConversationSequences.Remove(reservation.sequenceKey);
            if (progress.day == reservation.day) progress.actionsSpent = Math.Max(0, progress.actionsSpent - reservation.cost);
            progress.reservation = null; Changed?.Invoke();
        }
        public bool EndDay(out string reason)
        {
            if (!InvestigationOpen || progress.reservation != null) { reason = "Finish the conversation before ending the day."; return false; }
            if (progress.day >= Days) progress.phase = InvestigationPhase.Accusation;
            else { progress.day++; progress.actionsSpent = 0; }
            reason = InvestigationOpen ? "A new investigation day begins." : "The final day has ended. Review your evidence and construct an accusation.";
            Changed?.Invoke(); return true;
        }
        public float JourneyProgress => progress.phase == InvestigationPhase.Investigating ?
            Math.Min(.999f, (progress.day - 1 + (float)progress.actionsSpent / ActionsPerDay) / Days) : 1f;
        public ArrestRule EvaluateArrest(string suspect, IEnumerable<string> selection, out string reason)
        {
            if (progress.phase != InvestigationPhase.Accusation) { reason = "Accusations are available only after the final day ends."; return null; }
            var ids = selection.ToList();
            if (ids.Distinct().Count() != ids.Count || ids.Any(x => !progress.theories.Contains(x)))
            { reason = "Select distinct theories you have constructed."; return null; }
            var theories = ids.Select(catalogue.Theory).ToList();
            if (theories.Any(x => x == null)) { reason = "An accusation references missing theory data."; return null; }
            foreach (var rule in catalogue.arrestRules.Where(x => x.enabled && x.suspect == suspect))
            {
                if (ids.Count != rule.selectionCount || rule.requiredStatements.Any(x => !progress.statements.Contains(x)) ||
                    rule.requiredClues.Any(x => !progress.clues.Contains(x)) || rule.forbiddenFlags.Any(Has)) continue;
                var groups = theories.Where(x => !string.IsNullOrEmpty(x.observationGroup)).Select(x => x.observationGroup).Distinct().Count();
                // Assign each required proposition to a different observation group. One observation cannot corroborate itself.
                if (groups < rule.minimumObservationGroups || !Assign(rule.propositions, theories, 0, new HashSet<string>())) continue;
                reason = "Supported accusation: " + rule.id; return rule;
            }
            reason = "Provisional unresolved case: these theories do not support this accusation. Review the notebook and revise your selection.";
            return null;
        }
        static bool Assign(List<string> propositions, List<TheoryDefinition> theories, int index, HashSet<string> used)
        {
            if (index == propositions.Count) return true;
            foreach (var theory in theories.Where(x => x.propositions.Contains(propositions[index]) && !string.IsNullOrEmpty(x.observationGroup)))
            {
                if (!used.Add(theory.observationGroup)) continue;
                if (Assign(propositions, theories, index + 1, used)) return true;
                used.Remove(theory.observationGroup);
            }
            return false;
        }
    }
}
