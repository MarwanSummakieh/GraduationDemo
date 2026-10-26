using System.Linq;
using GraduationDemo.Editor;
using GraduationDemo.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class PacingAndNotebookTests
{
    NarrativeCatalogue catalogue;
    InvestigationConfiguration pacing;
    InvestigationState state;
    [SetUp] public void Setup()
    {
        catalogue = NarrativeImporter.Build(NarrativeImporter.Source); pacing = ScriptableObject.CreateInstance<InvestigationConfiguration>();
        pacing.actionCostsEnabled = true; state = new InvestigationState(catalogue, null, pacing);
    }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); Object.DestroyImmediate(pacing); }
    ConversationInitialization Prompt(string clue = "C01", string statement = "F-S01", string suspect = "Mila")
    {
        state.Grant(clue); state.Grant(statement); var t = state.Construct(clue, statement); return state.Presentation(t.id, suspect);
    }
    void Finish(ConversationInitialization prompt)
    {
        foreach (var beat in catalogue.Sequence(prompt.sequenceId).beats) foreach (var id in beat.grants) state.Grant(id, "sleeping_a");
        state.FinishConversation(prompt.selection);
    }
    [Test] public void AlibisAreFreeAndConsumedAtInitialization()
    {
        var alibi = state.Alibi("Mila"); Assert.That(alibi.cost, Is.Zero);
        Assert.That(state.TryInitialize(alibi, out _), Is.True); Assert.That(state.Alibi("Mila"), Is.Null);
        Assert.That(state.RemainingActions, Is.EqualTo(3)); Assert.That(state.TryInitialize(alibi, out _), Is.False);
        Finish(alibi); Assert.That(state.progress.statements.Count, Is.EqualTo(3));
    }
    [Test] public void DisabledCostsAllowConversationsPastBudgetAndCanBeReenabled()
    {
        pacing.actionCostsEnabled = false; pacing.alibiCost = 2; pacing.confrontationCost = 2;
        state.progress.actionsSpent = pacing.actionsPerDay;
        var prompt = Prompt(); Assert.That(prompt.cost, Is.Zero);
        Assert.That(state.TryInitialize(prompt, out _), Is.True); Finish(prompt);
        Assert.That(state.progress.actionsSpent, Is.EqualTo(pacing.actionsPerDay));
        Assert.That(state.Presentation(prompt.theoryId, prompt.suspect), Is.Null);
        pacing.actionCostsEnabled = true;
        Assert.That(state.Alibi("Mila").cost, Is.EqualTo(2));
        Assert.That(state.TryInitialize(state.Alibi("Mila"), out _), Is.False);
        state.EndDay(out _); Assert.That(state.TryInitialize(state.Alibi("Mila"), out _), Is.True);
    }
    [Test] public void NewPacingStartsWithCostsDisabled()
    {
        var defaults = ScriptableObject.CreateInstance<InvestigationConfiguration>();
        try { Assert.That(defaults.actionCostsEnabled, Is.False); }
        finally { Object.DestroyImmediate(defaults); }
    }
    [Test] public void OfferingDoesNotSpendAndConfirmationIsAtomic()
    {
        var prompt = Prompt(); Assert.That(state.RemainingActions, Is.EqualTo(3)); Assert.That(state.progress.usedInitializations, Is.Empty);
        Assert.That(state.TryInitialize(prompt, out _), Is.True); Assert.That(state.RemainingActions, Is.EqualTo(2));
        Assert.That(state.TryInitialize(prompt, out _), Is.False); Assert.That(state.RemainingActions, Is.EqualTo(2));
        Assert.That(state.EndDay(out _), Is.False); Finish(prompt); Assert.That(state.Presentation(prompt.theoryId, prompt.suspect), Is.Null);
    }
    [Test] public void BudgetDoesNotPreventFreeEvidenceOrAlibis()
    {
        state.progress.actionsSpent = 3; Assert.That(state.TryInitialize(Prompt(), out _), Is.False);
        Assert.That(state.Grant("C03", "odette"), Is.True); Assert.That(state.TryInitialize(state.Alibi("Felix"), out _), Is.True);
        state.FinishConversation(null); Assert.That(state.RemainingActions, Is.Zero);
    }
    [Test] public void DayClosureReplenishesThenLocksInvestigationAndUnlocksAccusation()
    {
        state.progress.actionsSpent = 1; Assert.That(state.EndDay(out _), Is.True); Assert.That(state.progress.day, Is.EqualTo(2)); Assert.That(state.RemainingActions, Is.EqualTo(3));
        state.EndDay(out _); Assert.That(state.progress.day, Is.EqualTo(3)); Assert.That(state.progress.phase, Is.EqualTo(InvestigationPhase.Investigating));
        Assert.That(state.EvaluateArrest("Mila", new string[0], out var reason), Is.Null); Assert.That(reason, Does.Contain("final day"));
        state.EndDay(out _); Assert.That(state.progress.phase, Is.EqualTo(InvestigationPhase.Accusation));
        Assert.That(state.Grant("C01"), Is.False); Assert.That(state.Alibi("Mila"), Is.Null); Assert.That(state.EndDay(out _), Is.False);
    }
    [Test] public void BalancingFieldsControlCostsAndDayCount()
    {
        pacing.investigationDays = 1; pacing.actionsPerDay = 5; pacing.alibiCost = 2; pacing.confrontationCost = 3;
        Assert.That(state.Alibi("Mila").cost, Is.EqualTo(2)); Assert.That(state.TryInitialize(Prompt(), out _), Is.True); Assert.That(state.RemainingActions, Is.EqualTo(2));
        state.FinishConversation(null); state.EndDay(out _); Assert.That(state.progress.phase, Is.EqualTo(InvestigationPhase.Accusation));
    }
    [Test] public void PlaybackFailureRestoresPromptAndSpentAction()
    {
        var prompt = Prompt(); state.TryInitialize(prompt, out _); state.RecoverFailedConversation();
        Assert.That(state.RemainingActions, Is.EqualTo(3)); Assert.That(state.Presentation(prompt.theoryId, prompt.suspect), Is.Not.Null);
        Assert.That(state.TryInitialize(prompt, out _), Is.True);
    }
    [Test] public void ChangedConditionsRejectAStaleConfirmationWithoutSpending()
    {
        var prompt = Prompt("C02", "M-S03", "Mila"); state.Grant("M-S04");
        Assert.That(state.TryInitialize(prompt, out _), Is.False); Assert.That(state.RemainingActions, Is.EqualTo(3));
    }
    [Test] public void NewConditionalResponseCanAppearAfterAPairWasUsed()
    {
        var first = Prompt(); state.TryInitialize(first, out _); Finish(first);
        catalogue.routes.Add(new RouteDefinition { id = "later/revelation", theoryId = first.theoryId, suspect = first.suspect, priority = 200,
            sequenceId = "An awkward guess", required = new System.Collections.Generic.List<string> { "flag:new_information" } });
        Assert.That(state.Presentation(first.theoryId, first.suspect), Is.Null);
        state.Grant("flag:new_information"); var later = state.Presentation(first.theoryId, first.suspect);
        Assert.That(later, Is.Not.Null); Assert.That(later.key, Is.Not.EqualTo(first.key));
        state.TryInitialize(later, out _); Finish(later); Assert.That(state.Presentation(first.theoryId, first.suspect), Is.Null);
    }
    [Test] public void SharedRevelationDisablesRedundantAlternativePrompts()
    {
        state.Grant("M-S04"); var first = Prompt("C07", "M-S04", "Mila"); state.TryInitialize(first, out _); Finish(first);
        Assert.That(Prompt("C10", "M-S04", "Mila"), Is.Null); Assert.That(state.progress.statements.Count(x => x == "M-S05"), Is.EqualTo(1));
    }
    [Test] public void ANewRouteCannotReplayTheSameSequenceForTheSameTheoryAndSuspect()
    {
        var first = Prompt(); state.TryInitialize(first, out _); Finish(first);
        catalogue.routes.Add(new RouteDefinition { id = "duplicate_sequence", theoryId = first.theoryId, suspect = first.suspect, priority = 200, sequenceId = first.sequenceId });
        Assert.That(state.Presentation(first.theoryId, first.suspect), Is.Null);
    }
    [Test] public void DifferentTheoriesMayUseTheSameGenericRecordingOnceEach()
    {
        var first = Prompt("C01", "F-S01", "Felix"); state.TryInitialize(first, out _); Finish(first);
        var second = Prompt("C01", "F-S02", "Felix"); Assert.That(second, Is.Not.Null);
        Assert.That(second.key, Is.Not.EqualTo(first.key)); state.TryInitialize(second, out _); Finish(second);
        Assert.That(state.Presentation(first.theoryId, "Felix"), Is.Null); Assert.That(state.Presentation(second.theoryId, "Felix"), Is.Null);
    }
    [Test] public void FirstAcquisitionLocationAndDayAreStable()
    {
        state.Grant("C01", "odette"); state.EndDay(out _); state.Grant("C01", "bar");
        var discovery = state.progress.discoveries.Single(x => x.id == "C01"); Assert.That(discovery.locationId, Is.EqualTo("odette")); Assert.That(discovery.day, Is.EqualTo(1));
        var legacy = new InvestigationProgress(); legacy.clues.Add("C02"); var restored = new InvestigationState(catalogue, legacy, pacing);
        Assert.That(restored.progress.discoveries.Single().locationId, Is.EqualTo("unknown"));
    }
    [Test] public void ProgressRoundTripRetainsPacingPromptsAndProvenance()
    {
        var prompt = Prompt(); state.TryInitialize(prompt, out _); Finish(prompt); state.EndDay(out _);
        var copy = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), pacing);
        Assert.That(copy.progress.usedInitializations, Is.EqualTo(state.progress.usedInitializations)); Assert.That(copy.progress.day, Is.EqualTo(2));
        Assert.That(copy.Presentation(prompt.theoryId, prompt.suspect), Is.Null); Assert.That(copy.progress.discoveries.Count, Is.EqualTo(state.progress.discoveries.Count));
    }
    [Test] public void FullGraphHasDistinctNodesAndRetainsEveryParent()
    {
        foreach (var clue in catalogue.clues) state.Grant(clue.id); foreach (var statement in catalogue.statements) state.Grant(statement.id);
        foreach (var clue in catalogue.clues) foreach (var statement in catalogue.statements) state.Construct(clue.id, statement.id);
        var graph = ReasoningGraphLayout.Build(catalogue, state.progress);
        Assert.That(graph.nodes.Count, Is.EqualTo(186)); Assert.That(graph.nodes.Select(n => n.id).Distinct().Count(), Is.EqualTo(186));
        Assert.That(graph.nodes.Select(n => n.position).Distinct().Count(), Is.EqualTo(186));
        foreach (var node in graph.nodes.Where(n => n.kind == ReasoningNodeKind.Theory)) { Assert.That(graph.nodes.Any(n => n.id == node.clueParent), Is.True); Assert.That(graph.nodes.Any(n => n.id == node.statementParent), Is.True); }
        var repeat = ReasoningGraphLayout.Build(catalogue, state.progress); Assert.That(repeat.nodes.Select(n => n.position), Is.EqualTo(graph.nodes.Select(n => n.position)));
    }
    [Test] public void GraphSearchPreservesSourcesAndSuspectClustering()
    {
        Prompt("C03", "B-S01", "Bellamy"); Prompt("C01", "F-S01", "Felix");
        var graph = ReasoningGraphLayout.Build(catalogue, state.progress, "head injury", "Bellamy");
        Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Theory), Is.EqualTo(1));
        Assert.That(graph.nodes.Any(n => n.id == "C03"), Is.True); Assert.That(graph.nodes.Any(n => n.id == "B-S01"), Is.True);
        Assert.That(graph.nodes.Where(n => n.kind == ReasoningNodeKind.Statement).All(n => n.speaker == "Bellamy"), Is.True);
    }
    [Test] public void InvalidPacingIsReported()
    {
        pacing.investigationDays = 0; pacing.actionsPerDay = 0; pacing.stops.Clear(); Assert.That(pacing.Errors().Count, Is.EqualTo(2));
    }
}
