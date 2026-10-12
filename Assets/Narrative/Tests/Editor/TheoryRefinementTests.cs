using System.Linq;
using GraduationDemo.Editor;
using GraduationDemo.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class TheoryRefinementTests
{
    NarrativeCatalogue catalogue;
    InvestigationState state;
    [SetUp] public void Setup() { catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue); foreach (var c in catalogue.clues) state.Grant(c.id); foreach (var s in catalogue.statements) state.Grant(s.id); }
    [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); }
    [Test] public void SingleTheoryUsesItsParentLineIntersection()
    {
        var t = state.Construct("C10", "M-S05"); var graph = ReasoningGraphLayout.Build(catalogue, state.progress);
        var node = graph.nodes.Single(n => n.id == t.id); var a = graph.nodes.Single(n => n.id == t.clueId); var b = graph.nodes.Single(n => n.id == t.statementId);
        float fraction = (node.position.x - a.position.x) / (b.position.x - a.position.x);
        Assert.That(node.position.x, Is.EqualTo(565)); Assert.That(node.position.y, Is.EqualTo(Mathf.Lerp(a.position.y, b.position.y, fraction)).Within(.01));
        Assert.That(node.position.y, Is.GreaterThan(graph.size.y * .6f));
    }
    [Test] public void FullGraphAvoidsOverlappingTheoryIconsAndCaptions()
    {
        foreach (var c in catalogue.clues) foreach (var s in catalogue.statements) state.Construct(c.id, s.id);
        var graph = ReasoningGraphLayout.Build(catalogue, state.progress); var nodes = graph.nodes.Where(n => n.kind == ReasoningNodeKind.Theory).ToArray();
        Assert.That(nodes.Length, Is.EqualTo(160));
        for (int i = 0; i < nodes.Length; i++) for (int j = i + 1; j < nodes.Length; j++)
            Assert.That(Mathf.Abs(nodes[i].position.x - nodes[j].position.x) >= 79.9f || Mathf.Abs(nodes[i].position.y - nodes[j].position.y) >= 95.9f, Is.True, nodes[i].id + " overlaps " + nodes[j].id);
        Assert.That(nodes.Any(n => n.position.y > graph.size.y * .7f), Is.True);
    }
    [Test] public void RemovalPreservesEvidenceAndConsumedConversationAndCanBeReconstructed()
    {
        var t = state.Construct("C01", "F-S01"); var prompt = state.Presentation(t.id, "Felix"); state.TryInitialize(prompt, out _); state.FinishConversation(prompt.selection);
        Assert.That(state.RemoveTheory(t.id), Is.True); Assert.That(state.progress.theories, Does.Not.Contain(t.id));
        Assert.That(state.Has("C01") && state.Has("F-S01"), Is.True); Assert.That(state.Presentation(t.id, "Felix"), Is.Null);
        Assert.That(state.Construct("C01", "F-S01").id, Is.EqualTo(t.id)); Assert.That(state.Presentation(t.id, "Felix"), Is.Null);
        Assert.That(state.progress.removedTheories, Is.Empty);
    }
    [Test] public void RemovedTheoriesCanBeRestoredAfterFinalClosureWithoutNewDiscovery()
    {
        var t = state.Construct("C03", "B-S01"); state.RemoveTheory(t.id); while (state.InvestigationOpen) state.EndDay(out _);
        Assert.That(state.RestoreTheory(t.id), Is.True); Assert.That(state.progress.theories, Does.Contain(t.id));
        Assert.That(state.Construct("C01", "F-S01"), Is.Null); Assert.That(state.RestoreTheory("T_C01_FS01"), Is.False);
        Assert.That(state.progress.phase, Is.EqualTo(InvestigationPhase.Accusation));
    }
    [Test] public void RemovalCannotInvalidateAnActiveConversation()
    {
        var t = state.Construct("C01", "F-S01"); state.TryInitialize(state.Presentation(t.id, "Felix"), out _);
        Assert.That(state.RemoveTheory(t.id), Is.False);
    }
}
