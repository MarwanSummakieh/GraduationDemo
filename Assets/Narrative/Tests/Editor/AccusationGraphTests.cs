using System.Linq;
using GraduationDemo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GraduationDemo.Narrative.Tests
{
    public class AccusationGraphTests
    {
        NarrativeCatalogue catalogue;
        InvestigationState state;
        [SetUp] public void Setup() { catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); }
        [Test] public void FinalClosureAllowsNewReasoningFromKnownSourcesButNoNewDiscoveriesOrInterviews()
        {
            state.Grant("C01"); state.Grant("F-S01");
            while (state.InvestigationOpen) state.EndDay(out _);
            Assert.That(state.Construct("C01", "F-S01"), Is.Not.Null);
            Assert.That(state.Construct("C02", "F-S01"), Is.Null);
            Assert.That(state.Grant("C02"), Is.False); Assert.That(state.Grant("F-S02"), Is.False);
            Assert.That(state.Alibi("Felix"), Is.Null); Assert.That(state.Presentation("T_C01_FS01", "Felix"), Is.Null);
            state.Grant("flag:resolved"); Assert.That(state.CanConstructTheories, Is.False);
            Assert.That(state.Construct("C01", "F-S01"), Is.Null);
        }
        [Test] public void AccusationGraphRetainsSourcesAndAddsDistinctDraftAndSuspectNodes()
        {
            state.Grant("C09"); state.Grant("F-S04"); var theory = state.Construct("C09", "F-S04");
            var graph = AccusationGraphLayout.Build(catalogue, state.progress, new[] { theory.id }, "Mila");
            Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Accusation), Is.EqualTo(1));
            Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Suspect), Is.EqualTo(3));
            Assert.That(graph.nodes.Select(n => n.id).Distinct().Count(), Is.EqualTo(graph.nodes.Count));
            Assert.That(graph.nodes.Any(n => n.id == theory.clueId), Is.True); Assert.That(graph.nodes.Any(n => n.id == theory.statementId), Is.True);
            Assert.That(graph.nodes.Single(n => n.id == AccusationGraphLayout.SuspectId("Felix")).detail, Does.Contain(catalogue.Statement("F-S04").claim));
            Assert.That(graph.nodes.Single(n => n.id == AccusationGraphLayout.SuspectId("Mila")).detail, Does.Not.Contain(catalogue.Statement("M-S05").claim));
        }
        [Test] public void FilteringCannotHideTetheredTheoriesOrTheirParents()
        {
            state.Grant("C09"); state.Grant("F-S04"); var theory = state.Construct("C09", "F-S04");
            var graph = AccusationGraphLayout.Build(catalogue, state.progress, new[] { theory.id }, "Felix", "nothing matches", "Mila");
            Assert.That(graph.nodes.Any(n => n.id == theory.id), Is.True);
            Assert.That(graph.nodes.Any(n => n.id == theory.clueId), Is.True); Assert.That(graph.nodes.Any(n => n.id == theory.statementId), Is.True);
            Assert.That(graph.nodes.Single(n => n.kind == ReasoningNodeKind.Accusation).detail, Does.Contain(theory.text));
        }
        [Test] public void FinalCaseCanBeConstructedEntirelyAfterInvestigationCloses()
        {
            foreach (var c in catalogue.clues) state.Grant(c.id); foreach (var s in catalogue.statements) state.Grant(s.id);
            while (state.InvestigationOpen) state.EndDay(out _);
            var theories = new[] { state.Construct("C03", "B-S01"), state.Construct("C09", "F-S04"), state.Construct("C10", "M-S05") };
            Assert.That(theories.All(t => t != null), Is.True);
            Assert.That(state.EvaluateArrest("Mila", theories.Select(t => t.id), out _), Is.Not.Null);
        }
    }
}
