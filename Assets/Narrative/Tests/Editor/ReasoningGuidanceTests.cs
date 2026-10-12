using NUnit.Framework;
using UnityEngine;

namespace GraduationDemo.Narrative.Tests
{
    public class ReasoningGuidanceTests
    {
        InvestigationConfiguration configuration;
        InvestigationProgress progress;
        [SetUp] public void Setup() { configuration = ScriptableObject.CreateInstance<InvestigationConfiguration>(); progress = new InvestigationProgress(); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(configuration); }
        [Test] public void GuidanceRequiresBothDiscoveriesAndDefaultsToSubtle()
        {
            Assert.That(configuration.reasoningAssistance, Is.EqualTo(ReasoningAssistance.Subtle));
            progress.statements.Add("M-S01");
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Guided), Is.Empty);
            progress.statements.Add("F-S04");
            var hints = ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle);
            Assert.That(hints.Count, Is.EqualTo(1)); Assert.That(hints[0].kind, Is.EqualTo(ReasoningHintKind.Contradiction));
            Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Subtle), Does.Not.Contain(hints[0].prompt));
            Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Guided), Does.Contain(hints[0].prompt));
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Off), Is.Empty);
        }
        [Test] public void AmbiguityHintsDoNotChangeDiscoveryOrActionState()
        {
            progress.clues.Add("C05"); progress.statements.Add("F-S01"); progress.actionsSpent = 2;
            string before = JsonUtility.ToJson(progress);
            var hints = ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Guided);
            Assert.That(hints.Count, Is.EqualTo(1)); Assert.That(hints[0].kind, Is.EqualTo(ReasoningHintKind.Ambiguity));
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(before));
        }
        [Test] public void AuthoredTheoryHintsRequireActiveTheoryNotArchivedTheory()
        {
            configuration.reasoningHints.Clear();
            configuration.reasoningHints.Add(new ReasoningHint { id = "custom", firstId = "T_C05_FS01", secondId = "C05", prompt = "Check the timing." });
            progress.clues.Add("C05"); progress.removedTheories.Add("T_C05_FS01");
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle), Is.Empty);
            progress.theories.Add("T_C05_FS01");
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle).Count, Is.EqualTo(1));
        }
    }
}
