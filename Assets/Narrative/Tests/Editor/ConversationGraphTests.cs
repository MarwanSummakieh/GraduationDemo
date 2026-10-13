using System.Linq;
using GraduationDemo.Editor;
using NUnit.Framework;
using UnityEngine;

namespace GraduationDemo.Narrative.Tests
{
    public class ConversationGraphTests
    {
        NarrativeCatalogue catalogue;
        InvestigationState state;
        [SetUp] public void Setup() { catalogue = NarrativeImporter.Build(NarrativeImporter.Source); state = new InvestigationState(catalogue); }
        [TearDown] public void Cleanup() { Object.DestroyImmediate(catalogue); }
        [Test] public void ConversationGraphContainsOnlyAvailableTheoriesAndTheirKnownParents()
        {
            state.Grant("C01"); state.Grant("F-S01"); state.Grant("C02");
            var theory = state.Construct("C01", "F-S01");
            var graph = ConversationGraphLayout.Build(state, "Felix");
            Assert.That(graph.nodes.Select(n => n.id), Is.EquivalentTo(new[] { theory.id, "C01", "F-S01" }));
            var conversation = state.Presentation(theory.id, "Felix"); state.TryInitialize(conversation, out _); state.FinishConversation(conversation.selection);
            Assert.That(ConversationGraphLayout.Build(state, "Felix").nodes, Is.Empty);
            Assert.That(ConversationGraphLayout.Build(state, "Mila").nodes.Any(n => n.id == theory.id), Is.True);
        }
        [Test] public void GuidanceConnectionFollowsConstructionRemovalAndRestorationWithoutLosingRecipientAdvice()
        {
            var configuration = ScriptableObject.CreateInstance<InvestigationConfiguration>();
            try
            {
                state.Grant("C06"); state.Grant("F-S02");
                var hint = ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed).Single();
                Assert.That(ReasoningGuidance.ConnectionUnbuilt(hint, catalogue, state.progress), Is.True);
                var theory = state.Construct("C06", "F-S02");
                Assert.That(ReasoningGuidance.ConnectionUnbuilt(hint, catalogue, state.progress), Is.False);
                Assert.That(ReasoningGuidance.Available(configuration, state.progress, ReasoningAssistance.Directed), Does.Contain(hint));
                Assert.That(ReasoningGuidance.Text(hint, ReasoningAssistance.Directed), Does.Contain("bring it to Felix"));
                state.RemoveTheory(theory.id); Assert.That(ReasoningGuidance.ConnectionUnbuilt(hint, catalogue, state.progress), Is.True);
                state.RestoreTheory(theory.id); Assert.That(ReasoningGuidance.ConnectionUnbuilt(hint, catalogue, state.progress), Is.False);
            }
            finally { Object.DestroyImmediate(configuration); }
        }
    }
}
