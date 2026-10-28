using System.Linq;
using System.Collections.Generic;
using GraduationDemo.Editor;
using GraduationDemo.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class DoubtDefenseTests
{
    NarrativeCatalogue catalogue;
    InvestigationConfiguration configuration;
    InvestigationState state;
    [SetUp] public void Setup()
    {
        catalogue = NarrativeImporter.Build(NarrativeImporter.Source);
        configuration = ScriptableObject.CreateInstance<InvestigationConfiguration>();
        configuration.doubtLinks = new List<AuthoredDoubtLink> { new AuthoredDoubtLink { id = "doubt:relationship", questionId = "T_C06_FS02", sourceId = "M-S03", reason = "Mila attributes the excitement to the masquerade rather than an engagement." } };
        state = new InvestigationState(catalogue, null, configuration);
    }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); Object.DestroyImmediate(configuration); }
    DoubtDefense Board()
    { state.Grant("C06"); state.Grant("F-S02"); state.Grant("M-S03"); state.Construct("C06", "F-S02"); return state.OpenDoubt("doubt:relationship"); }
    [Test] public void CoffeeFocusIsOneTimePersistentAndActuallyExpandsDeployment()
    {
        configuration.doubtTuning.focusBudget = 1; var board = Board();
        Assert.That(board.Place("C06", new Vector2(3, 3)), Is.True);
        Assert.That(board.Place("T_C06_FS02", new Vector2(4, 5)), Is.False);
        int clues = state.progress.clues.Count;
        Assert.That(state.ClaimCoffeeFocus(), Is.True); Assert.That(state.FocusCapacity, Is.EqualTo(3));
        Assert.That(board.Place("T_C06_FS02", new Vector2(4, 5)), Is.True);
        Assert.That(board.FocusUsed, Is.EqualTo(3)); Assert.That(state.ClaimCoffeeFocus(), Is.False);
        Assert.That(state.progress.clues.Count, Is.EqualTo(clues)); Assert.That(state.progress.actionsSpent, Is.Zero);
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), configuration);
        Assert.That(restored.FocusCapacity, Is.EqualTo(3)); Assert.That(restored.ClaimCoffeeFocus(), Is.False);
        restored.EndDay(out _); Assert.That(restored.FocusCapacity, Is.EqualTo(3));
    }
    [Test] public void CoffeeCanBeDisabledAndCannotBeClaimedDuringConversationOrAfterClosure()
    {
        configuration.coffeeFocusReward = 0; Assert.That(state.CoffeeAvailable, Is.False); Assert.That(state.ClaimCoffeeFocus(), Is.False);
        configuration.coffeeFocusReward = 4; var alibi = state.Alibi("Felix"); state.TryInitialize(alibi, out _);
        Assert.That(state.ClaimCoffeeFocus(), Is.False); state.RecoverFailedConversation(); Assert.That(state.ClaimCoffeeFocus(), Is.True);
        Assert.That(state.FocusCapacity, Is.EqualTo(10));
        state.progress.focusRewardClaims.Clear(); while (state.InvestigationOpen) state.EndDay(out _);
        Assert.That(state.ClaimCoffeeFocus(), Is.False);
    }
    [Test] public void TowersRetainExactFreePositionsIncludingTwoInTheSameFormerGridSquare()
    {
        var board = Board(); state.Grant("C01");
        Assert.That(board.Place("C06", new Vector2(3.1f, 3.1f)), Is.True);
        Assert.That(board.Place("C01", new Vector2(3.75f, 3.75f)), Is.True);
        Assert.That(board.Draft.towers[0].x, Is.EqualTo(3.1f).Within(.00001f));
        Assert.That(board.Draft.towers[1].y, Is.EqualTo(3.75f).Within(.00001f));
        Assert.That(board.Walls.Single().a, Is.EqualTo(new Vector2(3.75f, 3.75f)));
        Assert.That(board.RoutesOpen, Is.True);
        Assert.That(board.Place("C06", new Vector2(3.17f, 5.83f)), Is.True);
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), configuration);
        var tower = restored.OpenDoubt(board.Definition.id).Draft.towers.Single(t => t.entryId == "C06");
        Assert.That(tower.x, Is.EqualTo(3.17f).Within(.00001f)); Assert.That(tower.y, Is.EqualTo(5.83f).Within(.00001f));
    }
    [Test] public void FreePlacementChecksFootprintsAndPathClearanceWithoutSnapping()
    {
        var board = Board(); state.Grant("C01"); board.Place("C06", new Vector2(3.23f, 3.41f));
        Assert.That(board.Place("C01", new Vector2(3.3f, 3.5f)), Is.False);
        var random = new System.Random(93);
        foreach (var clue in catalogue.clues.Take(8))
        {
            state.Grant(clue.id);
            board.Place(clue.id, new Vector2(1 + (float)random.NextDouble() * 14, 1 + (float)random.NextDouble() * 10));
            Assert.That(board.RoutesOpen, Is.True);
        }
    }
    [Test] public void DoubtsRequireAnAuthoredQuestionAndItsContradictingSource()
    {
        Assert.That(DoubtRules.Available(state), Is.Empty); state.HintClue("C06", "Felix", "test"); state.Grant("F-S02");
        Assert.That(DoubtRules.Available(state), Is.Empty); state.Grant("C06");
        Assert.That(DoubtRules.Available(state), Is.Empty, "Gathered sources alone must not create doubts.");
        var question = state.Construct("C06", "F-S02"); Assert.That(DoubtRules.Available(state), Is.Empty);
        state.Grant("M-S03"); var doubt = DoubtRules.Available(state).Single();
        Assert.That(doubt.questionId, Is.EqualTo(question.id)); Assert.That(doubt.interpretation, Is.EqualTo(question.text));
        var suggestion = DoubtRules.Suggestion(state, doubt);
        Assert.That(state.Has(suggestion.clueId) && state.Has(suggestion.statementId), Is.True);
        Assert.That(state.progress.theories, Does.Not.Contain(suggestion.id));
        state.Grant("C01"); var other = state.Construct("C01", "F-S02");
        Assert.That(DoubtRules.Available(state).Any(d => d.questionId == other.id), Is.False);
        Assert.That(state.QuestionSecure(other.id), Is.True);
        Assert.That(state.QuestionDoubtResolved(other.id), Is.False, "An untouched secure question was never defended.");
        state.RemoveTheory(question.id); Assert.That(DoubtRules.Available(state).Any(d => d.questionId == question.id), Is.False);
        Assert.That(state.OpenDoubt(doubt.id), Is.Null); state.RestoreTheory(question.id); Assert.That(state.OpenDoubt(doubt.id), Is.Not.Null);
        Assert.That(state.OpenDoubt("unknown"), Is.Null);
    }
    [Test] public void FocusOnlyLimitsDeploymentAndMovementIsFree()
    {
        var board = Board(); configuration.doubtTuning.focusBudget = 2; var q = state.Construct("C06", "F-S02");
        Assert.That(board.Place(q.id, new Vector2Int(3, 3)), Is.True);
        Assert.That(board.Place("C06", new Vector2Int(4, 3)), Is.False);
        Assert.That(board.Place(q.id, new Vector2Int(4, 3)), Is.True); Assert.That(board.FocusUsed, Is.EqualTo(2));
        Assert.That(board.Place("C01", new Vector2Int(2, 3)), Is.False);
        Assert.That(board.Remove(q.id), Is.True); Assert.That(state.progress.theories, Does.Contain(q.id));
        Assert.That(state.progress.actionsSpent, Is.Zero); Assert.That(board.Place("C06", DoubtDefense.Center), Is.False);
    }
    [Test] public void AutoWallsRejectLoopsAndKeepEveryEntranceTraversable()
    {
        var board = Board(); state.Grant("C01"); state.Grant("C02");
        board.Place("C06", new Vector2Int(3, 3)); board.Place("C01", new Vector2Int(5, 3)); board.Place("C02", new Vector2Int(4, 5));
        Assert.That(board.Walls.Count(w => w.Accepted), Is.EqualTo(2));
        Assert.That(board.Walls.Any(w => w.rejection != null && w.rejection.Contains("loop")), Is.True);
        foreach (var entrance in DoubtDefense.Entrances)
        { var path = board.Path(entrance); Assert.That(path.First(), Is.EqualTo(entrance)); Assert.That(path.Last(), Is.EqualTo(DoubtDefense.Center)); }
        string signature = string.Join(";", board.Walls.Select(w => w.a + ":" + w.b + ":" + w.rejection));
        board.Draft.towers.Reverse(); board.RebuildWalls(); Assert.That(string.Join(";", board.Walls.Select(w => w.a + ":" + w.b + ":" + w.rejection)), Is.EqualTo(signature));
    }
    [Test] public void RandomArrangementsNeverCloseAPathIncludingLongWallRadii()
    {
        var board = Board(); configuration.doubtTuning.focusBudget = 12;
        foreach (var clue in catalogue.clues) state.Grant(clue.id);
        var random = new System.Random(47);
        for (int run = 0; run < 20; run++)
        {
            board.Draft.towers.Clear(); board.RebuildWalls(); configuration.doubtTuning.wallRadius = run % 2 == 0 ? 3.2f : 12;
            foreach (var clue in catalogue.clues.Take(8))
            { board.Place(clue.id, new Vector2Int(random.Next(1, 16), random.Next(1, 12))); Assert.That(board.RoutesOpen, Is.True, "Run " + run); }
        }
    }
    [Test] public void BuffAndSlowHaveMechanicalEffectsAndRehearsalNeverResolvesCase()
    {
        var board = Board(); var question = state.Construct("C06", "F-S02");
        board.Place("C06", new Vector2Int(3, 6)); board.Place("F-S02", new Vector2Int(2, 5)); board.Place(question.id, new Vector2Int(4, 5));
        Assert.That(board.Buffed(board.Draft.towers.First()), Is.True); Assert.That(board.Start(), Is.True); board.Step();
        Assert.That(board.Draft.invaders.Single().health, Is.LessThan(8 - configuration.doubtTuning.projectileDamage));
        Assert.That(board.Draft.invaders.Single().movement, Is.EqualTo(.5f));
        Assert.That(board.Effects.Select(e => e.kind), Does.Contain(FocusTowerKind.Projectile));
        Assert.That(board.Effects.Select(e => e.kind), Does.Contain(FocusTowerKind.Slow));
        for (int i = 0; i < 150 && !board.Draft.finished; i++) board.Step();
        Assert.That(board.Draft.finished, Is.True); Assert.That(state.Has("flag:resolved"), Is.False);
        Assert.That(state.progress.actionsSpent, Is.Zero);
    }
    [Test] public void DraftSimulationAndReflectionHistorySurviveJsonRoundtrip()
    {
        var board = Board(); board.Place("C06", new Vector2Int(3, 6)); board.Start(); board.Step();
        state.RecordDoubt(board, DoubtOutcome.Acknowledged, "Presence alone is insufficient.");
        var json = JsonUtility.ToJson(state.progress); var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(json), configuration);
        var reopened = restored.OpenDoubt(board.Definition.id);
        Assert.That(reopened.Draft.started, Is.True); Assert.That(reopened.Draft.tick, Is.EqualTo(1)); Assert.That(reopened.Draft.towers.Count, Is.EqualTo(1));
        Assert.That(reopened.Draft.invaders.Single().health, Is.EqualTo(board.Draft.invaders.Single().health));
        Assert.That(restored.progress.doubtReviews.Single().note, Does.Contain("insufficient"));
        reopened.Reset(); restored.RecordDoubt(reopened, DoubtOutcome.Revised, "Consider an innocent explanation.");
        Assert.That(restored.progress.doubtReviews.Count, Is.EqualTo(2)); Assert.That(restored.CanBuildAccusation, Is.True);
        Assert.That(reopened.Interpretation, Is.EqualTo("Consider an innocent explanation."));
        var second = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(restored.progress)), configuration);
        Assert.That(second.OpenDoubt(board.Definition.id).Interpretation, Is.EqualTo(reopened.Interpretation));
    }
    [Test] public void ArchivingActiveQuestionReopensChallengeInPlanningAndRetainsHistory()
    {
        var board = Board(); state.Grant("C01"); var question = state.Construct("C01", "F-S02");
        board.Place("C06", new Vector2Int(3, 6)); board.Place(question.id, new Vector2Int(4, 5)); board.Start(); board.Step();
        state.RemoveTheory(question.id); var reopened = state.OpenDoubt(board.Definition.id);
        Assert.That(reopened.Planning, Is.True); Assert.That(reopened.Draft.towers.Count, Is.EqualTo(1));
        Assert.That(state.progress.removedTheories, Does.Contain(question.id));
        state.RecordDoubt(reopened, DoubtOutcome.Examined, "Premature tactical result"); Assert.That(state.progress.doubtReviews, Is.Empty);
    }
    [Test] public void RoutesAndInvadersMoveContinuouslyAroundFreePositionedObstacles()
    {
        var board = Board(); Assert.That(board.Place("C06", new Vector2(3.23f, 6.17f)), Is.True);
        var path = board.Path(new Vector2(.17f, 5.91f)); Assert.That(path.Count, Is.GreaterThan(2));
        for (int i = 1; i < path.Count; i++) Assert.That(board.SegmentOpen(path[i - 1], path[i]), Is.True);
        Assert.That(path.Skip(1).Any(p => Mathf.Abs(p.x - Mathf.Round(p.x)) > .01f && Mathf.Abs(p.y - Mathf.Round(p.y)) > .01f), Is.True);
        board.Start(); board.Step(); var invader = board.Draft.invaders.Single();
        Assert.That(Mathf.Abs(invader.x - Mathf.Round(invader.x)), Is.GreaterThan(.01f));
        Assert.That(Mathf.Abs(invader.y - Mathf.Round(invader.y)), Is.GreaterThan(.01f));
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), configuration);
        var resumed = restored.OpenDoubt(board.Definition.id); board.Step(); resumed.Step();
        Assert.That(resumed.Draft.invaders.Single().x, Is.EqualTo(invader.x).Within(.00001f));
        Assert.That(resumed.Draft.invaders.Single().y, Is.EqualTo(invader.y).Within(.00001f));
    }
    [Test] public void InvalidTuningAndDuplicateDefinitionsAreRejected()
    {
        configuration.doubtTuning.focusBudget = 0; configuration.doubts.Add(configuration.doubts[0]);
        Assert.That(configuration.Errors().Any(e => e.Contains("focus")), Is.True);
        Assert.That(configuration.Errors().Any(e => e.Contains("Duplicate doubt")), Is.True);
    }
    [Test] public void LegacyGridRunResetsToPlanningWhileKeepingTheQuestionAndPlacement()
    {
        var board = Board(); board.Place("C06", new Vector2(3.23f, 6.17f)); board.Start(); board.Step();
        state.RecordDoubt(board, DoubtOutcome.Acknowledged, "Consider a different question."); board.Draft.navigationVersion = 0;
        var resumed = state.OpenDoubt(board.Definition.id);
        Assert.That(resumed.Planning, Is.True); Assert.That(resumed.Draft.invaders, Is.Empty);
        Assert.That(resumed.Draft.towers.Single().x, Is.EqualTo(3.23f));
        Assert.That(state.progress.doubtReviews.Single().outcome, Is.EqualTo(DoubtOutcome.Acknowledged));
        Assert.That(state.progress.theories, Does.Contain(resumed.Definition.questionId));
    }
    [Test] public void AccumulatedDoubtScalesOneBatchAndSuccessfulDefenseUnlocksEveryCapturedQuestion()
    {
        var board = Board();
        configuration.doubtLinks.Add(new AuthoredDoubtLink { id = "second", questionId = "T_C06_FS02", sourceId = "C03", reason = "A second authored counter-observation." });
        configuration.doubtLinks.Add(new AuthoredDoubtLink { id = "third", questionId = "T_C01_FS02", sourceId = "M-S03", reason = "A conflicting account affecting another question." });
        state.Grant("C03"); state.Grant("C01"); state.Construct("C01", "F-S02");
        Assert.That(state.DoubtPressure, Is.EqualTo(3)); board.RefreshPressure();
        Assert.That(board.Draft.waves, Is.EqualTo(2)); Assert.That(board.Draft.environment.Select(z => z.kind), Does.Contain(DoubtZoneKind.Obstruction));
        Assert.That(board.Draft.environment.Select(z => z.kind), Does.Contain(DoubtZoneKind.InvaderBuff));
        Assert.That(board.Draft.environment.Select(z => z.kind), Does.Contain(DoubtZoneKind.TowerDebuff));
        Assert.That(board.Place("C06", board.Draft.environment.First(z => z.kind == DoubtZoneKind.Obstruction).position), Is.False);
        Assert.That(board.InvaderEnvironmentMultiplier(board.Draft.environment.First(z => z.kind == DoubtZoneKind.InvaderBuff).position), Is.GreaterThan(1));
        Assert.That(board.TowerEnvironmentMultiplier(board.Draft.environment.First(z => z.kind == DoubtZoneKind.TowerDebuff).position), Is.LessThan(1));
        Assert.That(state.Presentation("T_C06_FS02", "Felix"), Is.Null);
        var locked = state.AvailableConversations("Felix").Single(p => p.theoryId == "T_C06_FS02");
        Assert.That(locked.blockedByDoubt, Is.True); Assert.That(state.TryInitialize(locked, out _), Is.False);
        configuration.doubtTuning.projectileRange = 50; configuration.doubtTuning.invaderHealth = .1f;
        board.Place("C06", new Vector2(6, 6)); Assert.That(board.Start(), Is.True);
        for (int i = 0; i < 150 && !board.Draft.finished; i++) board.Step();
        Assert.That(board.Draft.defended, Is.True); Assert.That(board.Draft.spawned, Is.EqualTo(6));
        Assert.That(state.DoubtPressure, Is.Zero); Assert.That(state.QuestionSecure("T_C01_FS02"), Is.True);
        Assert.That(state.QuestionDoubtResolved("T_C06_FS02"), Is.True); Assert.That(state.QuestionDoubtResolved("T_C01_FS02"), Is.True);
        Assert.That(state.Presentation("T_C06_FS02", "Felix"), Is.Not.Null);
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), configuration);
        Assert.That(restored.DoubtPressure, Is.Zero); Assert.That(restored.QuestionSecure("T_C06_FS02"), Is.True);
        Assert.That(restored.QuestionDoubtResolved("T_C06_FS02"), Is.True);
        configuration.doubtLinks.Add(new AuthoredDoubtLink { id = "later", questionId = "T_C06_FS02", sourceId = "C04", reason = "Newly encountered counter-evidence." }); state.Grant("C04");
        Assert.That(state.DoubtPressure, Is.EqualTo(1)); Assert.That(state.QuestionSecure("T_C06_FS02"), Is.False);
        Assert.That(state.QuestionDoubtResolved("T_C06_FS02"), Is.False, "New doubt must remove the resolved indicator.");
    }
    [Test] public void FailureAndReflectionNeverUnlockAndArchivedDoubtCannotErasePressure()
    {
        var board = Board(); configuration.doubtTuning.invaderHealth = 10000;
        board.Place("C06", new Vector2(1, 2)); board.Start();
        for (int i = 0; i < 150 && !board.Draft.finished; i++) board.Step();
        Assert.That(board.Draft.finished, Is.True); Assert.That(board.Draft.defended, Is.False);
        state.RecordDoubt(board, DoubtOutcome.Examined, "Still unsure."); Assert.That(state.QuestionSecure(board.Definition.questionId), Is.False);
        state.RemoveTheory(board.Definition.questionId); Assert.That(state.DoubtPressure, Is.EqualTo(1));
        state.RestoreTheory(board.Definition.questionId); Assert.That(state.Presentation(board.Definition.questionId, "Felix"), Is.Null);
    }
    [Test] public void NewContradictionInvalidatesAPaidConfirmationWithoutSpendingActions()
    {
        configuration.actionCostsEnabled = true; state.Grant("C06"); state.Grant("F-S02"); var q = state.Construct("C06", "F-S02");
        var offered = state.Presentation(q.id, "Felix"); Assert.That(offered, Is.Not.Null); Assert.That(offered.cost, Is.EqualTo(1));
        state.Grant("M-S03"); Assert.That(state.QuestionSecure(q.id), Is.False);
        Assert.That(state.TryInitialize(offered, out _), Is.False); Assert.That(state.progress.actionsSpent, Is.Zero);
        Assert.That(state.progress.reservation, Is.Null); Assert.That(state.progress.usedInitializations, Is.Empty);
        Assert.That(state.Alibi("Felix"), Is.Not.Null);
    }
    [TestCase(1)]
    [TestCase(100)]
    public void DefaultStoryCaseHasAWinningBalancedFormationAtAccumulatedPressure(int weight)
    {
        configuration.doubtLinks = DoubtRules.AuthoredLinks(); configuration.doubtLinks[0].weight = weight; state = new InvestigationState(catalogue, null, configuration);
        foreach (var id in new[] { "C02", "M-S04", "C10", "M-S05", "C07" }) state.Grant(id);
        var q = state.Construct("C02", "M-S04"); var board = state.OpenDoubt("clock:position");
        Assert.That(state.DoubtPressure, Is.EqualTo(weight + 2));
        Assert.That(board.Draft.waves, Is.LessThanOrEqualTo(configuration.doubtTuning.maxWaves));
        board.Place("C02", new Vector2(6, 6)); board.Place("C10", new Vector2(10, 6));
        board.Place("M-S04", new Vector2(8, 3)); board.Place(q.id, new Vector2(8, 9)); board.Place("C07", new Vector2(8, 4));
        Assert.That(board.FocusUsed, Is.LessThanOrEqualTo(6)); Assert.That(board.RoutesOpen, Is.True); Assert.That(board.Start(), Is.True);
        for (int i = 0; i < 250 && !board.Draft.finished; i++) board.Step();
        Assert.That(board.Draft.defended, Is.True); Assert.That(state.QuestionSecure(q.id), Is.True);
    }
    [TestCase(DoubtOutcome.Unresolved)]
    [TestCase(DoubtOutcome.Acknowledged)]
    [TestCase(DoubtOutcome.Revised)]
    public void NonCombatOutcomesAreLegitimateAndPersistWithoutSpending(DoubtOutcome outcome)
    {
        var board = Board(); board.Place("C06", new Vector2Int(3, 6));
        state.RecordDoubt(board, outcome, "This connection remains uncertain.");
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)), configuration);
        Assert.That(restored.progress.doubtReviews.Single().outcome, Is.EqualTo(outcome));
        Assert.That(restored.progress.actionsSpent, Is.Zero); Assert.That(restored.CanBuildAccusation, Is.True);
        Assert.That(restored.Has("flag:resolved"), Is.False); Assert.That(restored.Has("C06"), Is.True);
    }
}
