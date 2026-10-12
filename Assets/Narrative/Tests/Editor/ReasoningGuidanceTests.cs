using NUnit.Framework;
using UnityEngine;
using System.Linq;
using GraduationDemo.Editor;

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
            progress.statements.Add("F-S02");
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Guided), Is.Empty);
            progress.clues.Add("C06");
            var hints = ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle);
            Assert.That(hints.Count, Is.EqualTo(1)); Assert.That(hints[0].kind, Is.EqualTo(ReasoningHintKind.Ambiguity));
            Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Subtle), Does.Not.Contain(hints[0].prompt));
            Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Subtle), Does.Not.Contain(hints[0].secondId));
            Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Guided), Does.Contain(hints[0].prompt));
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Off), Is.Empty);
        }
        [Test] public void AmbiguityHintsDoNotChangeDiscoveryOrActionState()
        {
            progress.clues.Add("C09"); progress.statements.Add("F-S01"); progress.actionsSpent = 2;
            string before = JsonUtility.ToJson(progress);
            var hints = ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Guided);
            Assert.That(hints.Count, Is.EqualTo(1)); Assert.That(hints[0].kind, Is.EqualTo(ReasoningHintKind.Ambiguity));
            Assert.That(JsonUtility.ToJson(progress), Is.EqualTo(before));
        }
        [Test] public void DefaultLeadsCoverEveryFirstDisclosureWithoutRevealingItsRecipient()
        {
            var catalogue = NarrativeImporter.Build(NarrativeImporter.Source);
            try
            {
                var routes = catalogue.routes.Where(r => r.priority == 100).ToList();
                Assert.That(configuration.reasoningHints.Count(h => h.purpose == ReasoningHintPurpose.Interview), Is.EqualTo(routes.Count));
                foreach (var route in routes)
                {
                    var theory = catalogue.Theory(route.theoryId);
                    var hint = configuration.reasoningHints.Single(h => h.firstId == theory.clueId && h.secondId == theory.statementId);
                    Assert.That(hint.resolvedBy, Is.EquivalentTo(route.grants));
                    Assert.That(hint.recipient, Is.EqualTo(route.suspect));
                    var state = new InvestigationState(catalogue);
                    state.Grant(hint.firstId); state.Grant(hint.secondId);
                    state.Construct(hint.firstId, hint.secondId);
                    Assert.That(state.Presentation(theory.id, route.suspect).selection.route.id, Is.EqualTo(route.id));
                    Assert.That(hint.prompt, Does.Not.Contain("Present to"));
                    Assert.That(hint.prompt, Does.Not.Contain(route.suspect));
                    Assert.That(ReasoningGuidance.Text(hint, ReasoningAssistance.Guided), Does.Not.Contain("bring it to " + route.suspect));
                    Assert.That(ReasoningGuidance.Text(hint, ReasoningAssistance.Directed), Does.Contain("bring it to " + route.suspect));
                    foreach (var reward in route.grants) Assert.That(hint.prompt, Does.Not.Contain(reward));
                }
                Assert.That(configuration.reasoningHints.Any(h => h.firstId == "C05" && h.secondId == "F-S01"), Is.False);
            }
            finally { Object.DestroyImmediate(catalogue); }
        }
        [Test] public void SharedRevelationRetiresBothSpatialLeads()
        {
            progress.clues.AddRange(new[] { "C07", "C10" }); progress.statements.Add("M-S04");
            Assert.That(ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle).Count, Is.EqualTo(2));
            progress.statements.Add("M-S05");
            var hints = ReasoningGuidance.Available(configuration, progress, ReasoningAssistance.Subtle);
            Assert.That(hints.Count(h => h.purpose == ReasoningHintPurpose.Interview), Is.Zero);
            Assert.That(hints.Count(h => h.purpose == ReasoningHintPurpose.CaseReview), Is.EqualTo(2));
        }
        [Test] public void ReportedTheoryIsCaseEvidenceAndItsConfrontationsAreReactionOnly()
        {
            var catalogue = NarrativeImporter.Build(NarrativeImporter.Source);
            try
            {
                var state = new InvestigationState(catalogue); state.Grant("C09"); state.Grant("F-S04");
                var theory = state.Construct("C09", "F-S04");
                var hints = ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed).Where(h => ReasoningGuidance.Matches(h, theory)).ToList();
                Assert.That(hints.Count, Is.EqualTo(1)); Assert.That(hints[0].purpose, Is.EqualTo(ReasoningHintPurpose.CaseReview));
                Assert.That(ReasoningGuidance.Text(hints[0], ReasoningAssistance.Directed), Does.Contain("no further interview"));
                foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" })
                    Assert.That(ReasoningGuidance.InterviewNote(catalogue, state.progress, state.Presentation(theory.id, suspect)), Does.StartWith("No new testimony"));
                Assert.That(hints.Any(h => h.id == "rehearsal_bar_context"), Is.False, "A shared clue must not turn a different theory into an interview lead.");
            }
            finally { Object.DestroyImmediate(catalogue); }
        }
        [Test] public void EveryRevelationHasAnAcquiredOnlyFollowOnCaseConnection()
        {
            var catalogue = NarrativeImporter.Build(NarrativeImporter.Source);
            try
            {
                foreach (var reward in configuration.reasoningHints.Where(h => h.purpose == ReasoningHintPurpose.Interview).SelectMany(h => h.resolvedBy).Distinct())
                {
                    var review = configuration.reasoningHints.Where(h => h.purpose == ReasoningHintPurpose.CaseReview && h.secondId == reward).ToList();
                    Assert.That(review, Is.Not.Empty, "No follow-on connection for " + reward);
                    var p = new InvestigationProgress(); foreach (var c in catalogue.clues) p.clues.Add(c.id);
                    Assert.That(ReasoningGuidance.Available(configuration, p, ReasoningAssistance.Directed).Any(h => h.secondId == reward), Is.False);
                    p.statements.Add(reward);
                    Assert.That(ReasoningGuidance.Available(configuration, p, ReasoningAssistance.Directed).Any(h => review.Contains(h)), Is.True);
                }
            }
            finally { Object.DestroyImmediate(catalogue); }
        }
        [Test] public void DirectedLeadsContinueThroughNewStatementsToSupportedFinalCaseWithinBudget()
        {
            var catalogue = NarrativeImporter.Build(NarrativeImporter.Source);
            try
            {
                var state = new InvestigationState(catalogue, pacing: configuration);
                foreach (var clue in catalogue.clues) state.Grant(clue.id);
                foreach (var statement in catalogue.statements.Where(s => s.initial)) state.Grant(statement.id);
                int interviews = 0;
                for (int i = 0; i < 12; i++)
                {
                    var lead = ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed).FirstOrDefault(h => h.purpose == ReasoningHintPurpose.Interview);
                    if (lead == null) break;
                    if (state.RemainingActions == 0) Assert.That(state.EndDay(out _), Is.True);
                    var theory = state.Construct(lead.firstId, lead.secondId);
                    var conversation = state.Presentation(theory.id, lead.recipient);
                    Assert.That(conversation, Is.Not.Null);
                    Assert.That(ReasoningGuidance.InterviewNote(catalogue, state.progress, conversation), Does.Not.StartWith("No new"));
                    Assert.That(state.TryInitialize(conversation, out _), Is.True);
                    foreach (var beat in catalogue.Sequence(conversation.sequenceId).beats) foreach (var grant in beat.grants) state.Grant(grant);
                    state.FinishConversation(conversation.selection); interviews++;
                }
                Assert.That(interviews, Is.EqualTo(9));
                var selection = new[] { state.Construct("C03", "B-S01"), state.Construct("C09", "F-S04"), state.Construct("C10", "M-S05") };
                foreach (var theory in selection) Assert.That(ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed).Any(h => h.purpose == ReasoningHintPurpose.CaseReview && ReasoningGuidance.Matches(h, theory)), Is.True);
                while (state.InvestigationOpen) Assert.That(state.EndDay(out _), Is.True);
                Assert.That(state.EvaluateArrest("Mila", selection.Select(t => t.id), out _), Is.Not.Null);
                Assert.That(ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed).All(h => h.purpose == ReasoningHintPurpose.CaseReview), Is.True);
            }
            finally { Object.DestroyImmediate(catalogue); }
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
