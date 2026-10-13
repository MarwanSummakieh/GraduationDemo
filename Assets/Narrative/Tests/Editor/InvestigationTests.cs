using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Editor;
using GraduationDemo.Narrative;
using NUnit.Framework;
using UnityEngine;
using System;
using System.IO;
using UnityEditor;
using Object = UnityEngine.Object;

public sealed class InvestigationTests
{
    NarrativeCatalogue catalogue;
    InvestigationState state;
    [SetUp] public void SetUp() { catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(catalogue); }
    void Initial()
    {
        foreach (var c in catalogue.clues) state.Grant(c.id);
        foreach (var s in catalogue.statements.Where(x => x.initial)) state.Grant(s.id);
    }
    RouteSelection Present(string clue, string statement, string suspect)
    {
        state.Grant(clue); state.Grant(statement);
        if (state.RemainingActions == 0) state.EndDay(out _);
        var theory = state.Construct(clue, statement); var prompt = state.Presentation(theory.id, suspect);
        Assert.That(prompt, Is.Not.Null); Assert.That(state.TryInitialize(prompt, out _), Is.True); var selected = prompt.selection;
        Assert.That(selected.sequence, Is.Not.Null);
        // Simulate completed spoken beats, then completed encounter.
        foreach (var beat in selected.sequence.beats) foreach (var id in beat.grants) state.Grant(id);
        state.FinishConversation(selected); return selected;
    }
    [Test] public void AllPairsAndSuspectsResolveAndDataIsValid()
    {
        Assert.That(NarrativeValidation.Errors(catalogue), Is.Empty);
        Assert.That(catalogue.clues.Count, Is.EqualTo(10)); Assert.That(catalogue.statements.Count, Is.EqualTo(16));
        Assert.That(catalogue.theories.Count, Is.EqualTo(160)); Assert.That(catalogue.routes.Count(x => x.priority == 0), Is.EqualTo(480));
        foreach (var c in catalogue.clues) state.Grant(c.id);
        foreach (var s in catalogue.statements) state.Grant(s.id);
        foreach (var c in catalogue.clues) foreach (var s in catalogue.statements)
        {
            var t = state.Construct(c.id, s.id); Assert.That(t, Is.Not.Null);
            Assert.That(t.text, Is.Not.EqualTo(s.id + " | " + s.speaker));
            foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" }) Assert.That(catalogue.routes.Any(r => r.theoryId == t.id && r.suspect == suspect), Is.True);
        }
    }
    [Test] public void AcquisitionAndConstructionAreDistinctAndIdempotent()
    {
        Assert.That(state.Grant("C01"), Is.True); Assert.That(state.progress.clues, Does.Contain("C01"));
        Assert.That(state.Construct("C01", "F-S01"), Is.Null);
        Assert.That(state.progress.statements, Is.Empty);
        Assert.That(state.Grant("F-S01"), Is.True); Assert.That(state.Grant("F-S01"), Is.False);
        var first = state.Construct("C01", "F-S01"); Assert.That(state.Construct("F-S01", "C01"), Is.SameAs(first));
        Assert.That(state.progress.theories.Count, Is.EqualTo(1)); Assert.That(state.Has("flag:resolved"), Is.False);
    }
    [Test] public void ConstructingIngredientsNeverAwardsTestimony()
    {
        Initial(); state.Construct("C02", "M-S03"); Assert.That(state.Has("M-S04"), Is.False);
        Assert.That(state.SelectRoute("T_C02_MS03", "Mila").route.priority, Is.EqualTo(100));
    }
    [Test] public void SuspectSpecificRoutesAndDeterministicTieBreak()
    {
        Initial(); var t = state.Construct("C02", "M-S03");
        Assert.That(state.SelectRoute(t.id, "Mila").sequence.id, Is.EqualTo("An awkward guess"));
        Assert.That(state.SelectRoute(t.id, "Felix").route.suspect, Is.EqualTo("Felix"));
        var candidate = state.SelectRoute(t.id, "Mila").route;
        var clone = JsonUtility.FromJson<RouteDefinition>(JsonUtility.ToJson(candidate)); clone.id = "AAA"; catalogue.routes.Add(clone);
        Assert.That(state.SelectRoute(t.id, "Mila").route.id, Is.EqualTo("AAA"));
    }
    [TestCase("C07", "C10")][TestCase("C10", "C07")]
    public void AlternativeSpatialRevelationsShareOnceOnlyReward(string first, string second)
    {
        Initial(); Present("C02", "M-S03", "Mila"); Assert.That(state.Has("M-S04"), Is.True);
        var selected = Present(first, "M-S04", "Mila"); Assert.That(selected.route.priority, Is.EqualTo(100));
        Assert.That(state.Has("M-S05"), Is.True);
        var newTheory = state.Construct("C10", "M-S05"); Assert.That(newTheory, Is.Not.Null);
        var redundant = state.Construct(second, "M-S04"); Assert.That(state.Presentation(redundant.id, "Mila"), Is.Null);
        Assert.That(state.progress.statements.Count(x => x == "M-S05"), Is.EqualTo(1));
        state.Complete(selected); Assert.That(state.progress.completedRoutes.Count(x => x == selected.route.id), Is.EqualTo(1));
    }
    [Test] public void EveryRevelationIsReachableThroughPlayableRules()
    {
        Initial();
        foreach (var pair in new[] { new[] { "C02", "M-S03", "Mila" }, new[] { "C07", "M-S04", "Mila" }, new[] { "C06", "F-S02", "Felix" }, new[] { "C09", "F-S01", "Felix" }, new[] { "C08", "F-S01", "Felix" }, new[] { "C09", "B-S02", "Bellamy" }, new[] { "C02", "B-S02", "Bellamy" }, new[] { "C08", "B-S01", "Bellamy" }, new[] { "C06", "M-S03", "Mila" } }) Present(pair[0], pair[1], pair[2]);
        Assert.That(state.progress.statements.Count, Is.EqualTo(16));
    }
    [TestCase("T_C03_BS01", "T_C04_FS04", "T_C10_MS05")]
    [TestCase("T_C02_BS01", "T_C09_FS04", "T_C07_MS05")]
    public void IndependentPhysicalBuildsSupportStaticEnding(string injury, string presence, string contradiction)
    {
        PrepareArrest(injury, presence, contradiction);
        var rule = state.EvaluateArrest("Mila", new[] { injury, presence, contradiction }, out _);
        Assert.That(rule, Is.Not.Null); Assert.That(rule.endingSequence, Is.EqualTo(NarrativeImporter.Ending));
        Assert.That(state.Has("flag:resolved"), Is.False);
    }
    void PrepareArrest(params string[] ids)
    {
        Initial(); state.Grant("F-S04"); state.Grant("M-S05");
        foreach (var id in ids) { var t = catalogue.Theory(id); state.Construct(t.clueId, t.statementId); }
        while (state.InvestigationOpen) state.EndDay(out _);
    }
    [Test] public void WrongSuspectRedundancyMissingEvidenceAndDisclosureCannotSucceed()
    {
        var ids = new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }; PrepareArrest(ids);
        Assert.That(state.EvaluateArrest("Felix", ids, out _), Is.Null);
        Assert.That(state.EvaluateArrest("Mila", new[] { ids[0], ids[0], ids[2] }, out _), Is.Null);
        state.progress.statements.Remove("M-S02"); Assert.That(state.EvaluateArrest("Mila", ids, out _), Is.Null); state.progress.statements.Add("M-S02");
        state.Grant("flag:geometry_disclosed"); Assert.That(state.EvaluateArrest("Mila", ids, out _), Is.Null);
    }
    [Test] public void OneObservationCannotSatisfyIndependentPropositions()
    {
        var ids = new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }; PrepareArrest(ids);
        catalogue.Theory(ids[2]).observationGroup = catalogue.Theory(ids[1]).observationGroup;
        Assert.That(state.EvaluateArrest("Mila", ids, out _), Is.Null);
    }
    [Test] public void MissingAssetsAndCircularDependenciesAreActionable()
    {
        catalogue.Sequence("An awkward guess").beats[0].voiceId = "MISSING";
        foreach (var route in catalogue.routes.Where(x => x.grants.Contains("M-S04"))) route.required.Add("M-S05");
        var errors = NarrativeValidation.Errors(catalogue);
        Assert.That(errors.Any(x => x.Contains("Missing voice MISSING")), Is.True);
        Assert.That(errors.Any(x => x.Contains("Unreachable statement / circular dependency M-S04")), Is.True);
    }
    [Test] public void CsvSupportsQuotedNewlinesCommasAndDoubleQuotes()
    {
        var rows = NarrativeImporter.ParseCsv("a,b\r\n\"hello,\nworld\",\"a\"\"b\"\r\n");
        Assert.That(rows[1][0], Is.EqualTo("hello,\nworld")); Assert.That(rows[1][1], Is.EqualTo("a\"b"));
    }
    [Test] public void IncompleteRoutesUseSafeSuspectFallbackWithoutRewards()
    {
        Initial(); var theory = state.Construct("C01", "F-S01");
        catalogue.routes.RemoveAll(x => x.theoryId == theory.id && x.suspect == "Felix");
        var selection = state.SelectRoute(theory.id, "Felix");
        Assert.That(selection.sequence.id, Is.EqualTo("Reusable/Felix")); Assert.That(selection.route, Is.Null);
        state.Complete(selection); Assert.That(state.progress.statements.Count, Is.EqualTo(7));
    }
    [Test] public void ReleaseGateRejectsMissingMatchingRecordings()
    {
        Assert.DoesNotThrow(() => NarrativeBuildGuard.ValidateForBuild(catalogue, true));
        var error = Assert.Throws<UnityEditor.Build.BuildFailedException>(() => NarrativeBuildGuard.ValidateForBuild(catalogue, false));
        Assert.That(error.Message, Does.Contain("Release build blocked"));
        Assert.That(error.Message, Does.Contain("MILA_CANON_FINALE_01"));
    }
    [Test] public void RefreshPreservesDesignerMetadataAndOriginalVoices()
    {
        catalogue.Theory("T_C01_FS01").propositions.Add("CUSTOM");
        var clip = AudioClip.Create("Import preservation test", 100, 1, 44100, false);
        catalogue.Voice("BURR_001").clip = clip; catalogue.Voice("MILA_CANON_FINALE_01").clip = clip;
        var refresh = NarrativeImporter.Build(NarrativeImporter.Source, catalogue);
        try { Assert.That(refresh.Theory("T_C01_FS01").propositions, Does.Contain("CUSTOM")); Assert.That(refresh.Voice("MILA_008").subtitle, Does.Contain("desk")); Assert.That(refresh.Voice("MILA_CANON_FINALE_01").subtitle, Does.Contain("heavy clock")); Assert.That(refresh.Voice("BURR_001").clip, Is.SameAs(clip)); Assert.That(refresh.Voice("MILA_CANON_FINALE_01").clip, Is.SameAs(clip)); }
        finally { Object.DestroyImmediate(refresh); Object.DestroyImmediate(clip); }
    }
    [Test] public void ProgressRoundTripPreservesDiscoveriesAndFlags()
    {
        Initial(); Present("C02", "M-S03", "Mila"); state.Grant("flag:investigation");
        var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)));
        Assert.That(restored.progress.theories, Is.EqualTo(state.progress.theories)); Assert.That(restored.progress.statements, Is.EqualTo(state.progress.statements));
        Assert.That(restored.progress.completedRoutes, Is.EqualTo(state.progress.completedRoutes)); Assert.That(restored.Has("flag:investigation"), Is.True);
    }
    [Test] public void PrematureGeometryDisclosureAndImpossibleIngredientsAreRejected()
    {
        catalogue.Voice("BURR_019").disclosesFallGeometry = true;
        catalogue.routes[0].forbidden.Add(catalogue.Theory(catalogue.routes[0].theoryId).clueId);
        var errors = NarrativeValidation.Errors(catalogue);
        Assert.That(errors.Any(x => x.Contains("Confidential geometry in opening / initial interview")), Is.True);
        Assert.That(errors.Any(x => x.Contains("Route forbids its own ingredient")), Is.True);
    }
    [Test] public void RepeatedRefreshKeepsCatalogueGuidAndVoiceReferences()
    {
        var existing = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(NarrativeImporter.AssetPath);
        Assert.That(existing, Is.Not.Null);
        string guid = AssetDatabase.AssetPathToGUID(NarrativeImporter.AssetPath);
        var clips = existing.voices.ToDictionary(x => x.id, x => x.clip);
        NarrativeImporter.Refresh(); NarrativeImporter.Refresh();
        Assert.That(AssetDatabase.AssetPathToGUID(NarrativeImporter.AssetPath), Is.EqualTo(guid));
        Assert.That(AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(NarrativeImporter.AssetPath), Is.SameAs(existing));
        foreach (var voice in existing.voices) if (clips.ContainsKey(voice.id)) Assert.That(voice.clip, Is.EqualTo(clips[voice.id]));
    }
    [Test] public void InvalidRefreshLeavesExistingCatalogueUntouched()
    {
        var existing = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(NarrativeImporter.AssetPath);
        string before = EditorJsonUtility.ToJson(existing);
        string directory = Path.Combine(Path.GetTempPath(), "GraduationNarrativeTest_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var file in Directory.GetFiles(NarrativeImporter.Source, "*.csv")) File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
            File.AppendAllText(Path.Combine(directory, "clues.csv"), "\nC01,Duplicate,Fact,Location,Notes\n");
            Assert.Throws<InvalidDataException>(() => NarrativeImporter.RefreshFrom(directory));
            Assert.That(EditorJsonUtility.ToJson(existing), Is.EqualTo(before));
        }
        finally
        {
            string resolved = Path.GetFullPath(directory);
            string temporaryRoot = Path.GetFullPath(Path.GetTempPath());
            if (!resolved.StartsWith(temporaryRoot, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(resolved).StartsWith("GraduationNarrativeTest_"))
                throw new InvalidOperationException("Refusing to delete outside the narrative test temporary directory.");
            Directory.Delete(resolved, true);
        }
    }
}
