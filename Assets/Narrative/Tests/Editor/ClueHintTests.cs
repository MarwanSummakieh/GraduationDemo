using System.Linq;
using GraduationDemo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GraduationDemo.Narrative.Tests
{
    public sealed class ClueHintTests
    {
        NarrativeCatalogue catalogue;
        InvestigationState state;
        [SetUp] public void Setup() { catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); }
        [Test] public void HintsRevealSearchLeadsWithoutCollectingEvidenceOrEnablingReasoning()
        {
            Assert.That(state.CanFindClue("C05"), Is.False);
            Assert.That(state.DiscoverClue("C05", "bar"), Is.False);
            Assert.That(state.HintClue("C05", "Felix", "FELI_002"), Is.True);
            Assert.That(state.CanFindClue("C05"), Is.True); Assert.That(state.Has("C05"), Is.False);
            Assert.That(state.progress.discoveries, Is.Empty);
            state.Grant("F-S01"); Assert.That(state.Construct("C05", "F-S01"), Is.Null);
            Assert.That(state.HasAccusationEntry("C05"), Is.False);
            var node = ReasoningGraphLayout.Build(catalogue, state.progress).nodes.Single(n => n.id == "C05");
            Assert.That(node.anticipated, Is.True); Assert.That(node.detail, Does.Contain("Felix").And.Contain("not collected"));
            Assert.That(node.detail, Does.Not.Contain(catalogue.Clue("C05").fact));
        }
        [Test] public void FindingAClueUpgradesItsSingleNodeAndRecordsPhysicalProvenance()
        {
            state.HintClue("C05", "Felix", "FELI_002"); state.EndDay(out _);
            Assert.That(state.DiscoverClue("C05", "bar"), Is.True);
            var graph = ReasoningGraphLayout.Build(catalogue, state.progress);
            Assert.That(graph.nodes.Count(n => n.id == "C05"), Is.EqualTo(1));
            Assert.That(graph.nodes.Single(n => n.id == "C05").anticipated, Is.False);
            Assert.That(graph.nodes.Single(n => n.id == "C05").detail, Is.EqualTo(catalogue.Clue("C05").fact));
            Assert.That(state.progress.discoveries.Single().locationId, Is.EqualTo("bar"));
            Assert.That(state.progress.discoveries.Single().day, Is.EqualTo(2));
            Assert.That(state.HintClue("C05", "Mila", "other"), Is.False);
        }
        [Test] public void HintsDeduplicateSurviveSaveAndDoNotUnlockAfterInvestigation()
        {
            state.HintClue("C08", "Mila", "MILA_004");
            Assert.That(state.HintClue("C08", "Mila", "MILA_004"), Is.False);
            var restored = new InvestigationState(catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(state.progress)));
            Assert.That(restored.CanFindClue("C08"), Is.True);
            Assert.That(restored.progress.reservation, Is.Null, "A save without a conversation must restore without a reservation.");
            Assert.That(restored.progress.clueHints.Single().voiceId, Is.EqualTo("MILA_004"));
            for (int i = 0; i < restored.Days && restored.InvestigationOpen; i++) Assert.That(restored.EndDay(out var reason), Is.True, reason);
            Assert.That(restored.HintClue("C05", "Felix", "FELI_002"), Is.False);
            Assert.That(restored.DiscoverClue("C08", "dining"), Is.False);
        }
        [Test] public void AuthoredHiddenCluesHaveSpokenNonCircularHintRoutes()
        {
            Assert.That(catalogue.clues.Where(c => c.hiddenUntilHint).Select(c => c.id), Is.EquivalentTo(new[] { "C05", "C08", "C10" }));
            foreach (var cue in new[] { new[] { "FELI_002", "C05" }, new[] { "MILA_004", "C08" }, new[] { "MILA_CANON_SPATIAL_01", "C10" } })
                Assert.That(catalogue.sequences.SelectMany(s => s.beats).Any(b => b.voiceId == cue[0] && b.clueHints.Contains(cue[1])), Is.True);
            Assert.That(NarrativeValidation.Errors(catalogue), Is.Empty);
            foreach (var beat in catalogue.sequences.SelectMany(s => s.beats)) beat.clueHints.Remove("C05");
            Assert.That(NarrativeValidation.Errors(catalogue), Does.Contain("Hidden clue has no dialogue hint: C05"));
        }
        [Test] public void LegacyRouteClueRewardsBecomeHintsAndGuidanceRecognizesNewLeads()
        {
            foreach (var statement in catalogue.statements.Where(s => s.initial)) state.Grant(statement.id);
            var prompt = new ConversationInitialization { kind = ConversationKind.Theory, suspect = "Felix", sequenceId = "Felix — Champagne alibi" };
            Assert.That(ReasoningGuidance.InterviewNote(catalogue, state.progress, prompt), Does.Not.StartWith("No new"));
            state.Complete(new RouteSelection { route = new RouteDefinition { id = "legacy-hint-route", suspect = "Felix", grants = new System.Collections.Generic.List<string> { "C05" } }, sequence = catalogue.Sequence(prompt.sequenceId) });
            Assert.That(state.HasClueHint("C05"), Is.True); Assert.That(state.Has("C05"), Is.False);
            Assert.That(state.progress.clueHints.Single().voiceId, Is.EqualTo("FELI_002"));
            Assert.That(ReasoningGuidance.InterviewNote(catalogue, state.progress, prompt), Does.StartWith("No new"));
        }
        [Test] public void HintValidationRejectsUnknownReferencesAndCircularLeads()
        {
            catalogue.sequences[0].beats[0].clueHints.Add("missing");
            catalogue.Clue("C06").hiddenUntilHint = true; catalogue.Clue("C06").hint = "Look for rings.";
            // The only new ring hint needs a question constructed from the rings themselves.
            catalogue.routes.First(r => r.theoryId == "T_C06_FS02").sequenceId = "Circular hint";
            catalogue.sequences.Add(new DialogueDefinition { id = "Circular hint", beats = new System.Collections.Generic.List<DialogueBeat> {
                new DialogueBeat { voiceId = "FELI_002", clueHints = new System.Collections.Generic.List<string> { "C06" } } } });
            var errors = NarrativeValidation.Errors(catalogue);
            Assert.That(errors.Any(e => e.Contains("Unknown clue hint missing")), Is.True);
            Assert.That(errors, Does.Contain("Unreachable clue hint / circular dependency C06"));
        }
    }
}
