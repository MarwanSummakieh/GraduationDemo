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
        public ArrestRule EvaluateArrest(string suspect, IEnumerable<string> selection, out string reason, IReadOnlyDictionary<string, string> sockets = null)
        {
            var assessment = AssessAccusation(suspect, selection, sockets);
            reason = assessment.Reason; return assessment.Supported ? assessment.rule : null;
        }
        public AccusationAssessment AssessAccusation(string suspect, IEnumerable<string> selection, IReadOnlyDictionary<string, string> sockets = null)
        {
            var result = new AccusationAssessment();
            if (progress.phase != InvestigationPhase.Accusation)
            { result.Add("phase", "Final day closed", false, "Accusations are available only after the final day ends."); return result; }
            var ids = (selection ?? Enumerable.Empty<string>()).ToList();
            if (ids.Distinct().Count() != ids.Count || ids.Any(id => !progress.theories.Contains(id) || catalogue.Theory(id) == null))
            { result.Add("selection", "Active distinct theories", false, "Select distinct theories you have constructed."); return result; }
            var theories = ids.Select(catalogue.Theory).ToList();
            var rules = catalogue.arrestRules.Where(r => r.enabled && (string.IsNullOrEmpty(suspect) || r.suspect == suspect)).ToList();
            if (rules.Count == 0)
            { result.Add("suspect", "Suspect support", false, "These theories do not support the chosen suspect's responsibility under the case criteria."); return result; }
            var assessments = new List<AccusationAssessment>();
            foreach (var rule in rules)
            {
                var a = new AccusationAssessment { rule = rule };
                a.Add("count", "Theory tethers", ids.Count == rule.selectionCount, "Tether " + rule.selectionCount + " distinct theories.");
                a.Add("suspect", "Suspect tether", suspect == rule.suspect, "Tether the accusation to a suspect.");
                a.Add("sources", "Collected source evidence", theories.All(t => progress.clues.Contains(t.clueId) && progress.statements.Contains(t.statementId)), "An attached theory is missing its collected source evidence.");
                if (sockets != null) a.Add("sockets", "One theory per socket", sockets.Count == rule.selectionCount && rule.propositions.All(sockets.ContainsKey) &&
                    sockets.Values.Distinct().Count() == ids.Count && sockets.Values.All(ids.Contains), "Attach one distinct theory to each Means, Motive and Opportunity socket.");
                foreach (var proposition in rule.propositions)
                {
                    string label = PropositionLabel(proposition);
                    bool covered = sockets == null ? theories.Any(t => t.propositions.Contains(proposition)) :
                        sockets.TryGetValue(proposition, out var attached) && ids.Contains(attached) && catalogue.Theory(attached).propositions.Contains(proposition);
                    a.Add(proposition, label, covered, "Missing evidence supporting " + label.ToLowerInvariant() + ".", true);
                }
                int missingClues = rule.requiredClues.Count(id => !progress.clues.Contains(id));
                int missingStatements = rule.requiredStatements.Count(id => !progress.statements.Contains(id));
                a.Add("clues", "Physical observations", missingClues == 0, "Required physical observations not collected: " + missingClues + ".");
                a.Add("statements", "Witness accounts", missingStatements == 0, "Required witness accounts not collected: " + missingStatements + ".");
                bool independent = theories.Where(t => !string.IsNullOrEmpty(t.observationGroup)).Select(t => t.observationGroup).Distinct().Count() >= rule.minimumObservationGroups &&
                    Assign(rule.propositions, theories, 0, new HashSet<string>());
                a.Add("independence", "Independent corroboration", independent, "Each required evidence line needs a separate supporting observation; an uncorroborated interpretation does not add one.");
                a.Add("conditions", "Usable evidence", !rule.forbiddenFlags.Any(Has), progress.flags.Contains("flag:geometry_disclosed") ?
                    "The private geometry was disclosed; knowledge of it no longer establishes a contradiction." : "This case cannot currently be resolved from these theories.");
                if (ids.Contains("T_C06_MS06")) a.notes.Add("Motive is a plausible interpretation of the relationship and reaction, not proof of intent. This is an investigative hypothesis.");
                assessments.Add(a);
            }
            return assessments.OrderBy(a => a.checks.Count(c => !c.satisfied)).First();
        }
        static string PropositionLabel(string proposition)
        {
            switch (proposition)
            {
                case AccusationReasoning.Means: return "Means";
                case AccusationReasoning.Motive: return "Motive";
                case AccusationReasoning.Opportunity: return "Opportunity";
                case "P1_REAL_INJURY": return "Real injury";
                case "P3_MILA_PRESENCE": return "Presence at the scene";
                case "P4_CONTRADICTORY_ACCOUNT": return "Contradictory testimony";
                default: return "Required evidence line";
            }
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
