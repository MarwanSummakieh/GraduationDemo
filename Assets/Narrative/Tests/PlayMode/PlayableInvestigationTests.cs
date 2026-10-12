using System;
using System.Collections;
using System.Linq;
using GraduationDemo.Narrative;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class PlayableInvestigationTests
{
    Component controller, tour;
    InvestigationState State => (InvestigationState)controller.GetType().GetProperty("State").GetValue(controller);
    string Sequence => (string)controller.GetType().GetProperty("CurrentSequence").GetValue(controller);
    object Call(Component target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target, args);
    [UnitySetUp] public IEnumerator SetUp()
    {
        yield return SceneManager.LoadSceneAsync("CarnivalExpress"); yield return null;
        controller = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x => x.GetType().Name == "TrainNarrative");
        tour = controller.GetComponent("TrainTour"); Assert.That(State, Is.Not.Null);
    }
    void Finish()
    {
        for (int i = 0; i < 50 && Sequence != null; i++) Call(controller, "Advance");
        Assert.That(Sequence, Is.Null);
    }
    [UnityTest] public IEnumerator OpeningAndFiveCarriageNavigation()
    {
        Assert.That(Sequence, Is.EqualTo("OPENING — The real corpse"));
        Assert.That(State.Has("flag:investigation"), Is.False); Assert.That(State.progress.statements, Is.Empty);
        Finish(); Assert.That(State.Has("flag:investigation"), Is.True);
        var views = (Transform[])tour.GetType().GetField("roomViews").GetValue(tour);
        Assert.That(views.Length, Is.EqualTo(5));
        var detective = (CharacterController)tour.GetType().GetField("detective").GetValue(tour);
        for (int i = 0; i < 5; i++)
        {
            Call(tour, "SetView", i, true); Call(tour, "SnapCamera");
            Assert.That(detective.transform.position.x, Is.EqualTo(views[i].position.x).Within(.05));
        }
        yield return null;
    }
    [UnityTest] public IEnumerator ContinuousWalkingAisleAcrossAllFiveCarriages()
    {
        Finish(); var detective = (CharacterController)tour.GetType().GetField("detective").GetValue(tour);
        detective.enabled = false; detective.transform.position = new Vector3(-30, 1.12f, -.7f); detective.enabled = true;
        for (int i = 0; i < 600; i++) detective.Move(new Vector3(.1f, -.03f, 0));
        Assert.That(detective.transform.position.x, Is.GreaterThan(29), "Continuous aisle blocked at " + detective.transform.position);
        Assert.That(detective.transform.position.y, Is.GreaterThan(.8f), "Floor gap in investigation aisle"); yield return null;
    }
    [UnityTest] public IEnumerator InteractionsAwardOnlySpokenStatementsAndPhysicalClues()
    {
        Finish();
        var bindings = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(x => x.GetType().Name == "NarrativeInteraction").ToArray();
        foreach (var binding in bindings)
        {
            string clue = (string)binding.GetType().GetField("clueId").GetValue(binding);
            if (string.IsNullOrEmpty(clue)) continue;
            Call(tour, "OpenObservation", binding.GetComponent("TrainObservation"));
            Assert.That(State.progress.clues, Does.Contain(clue));
            Call(tour, "CloseObservation");
        }
        Assert.That(State.progress.clues.Count, Is.EqualTo(10)); Assert.That(State.progress.statements, Is.Empty);
        foreach (var suspect in new[] { "Felix", "Bellamy", "Mila" })
        {
            Approach(suspect);
            var binding = bindings.First(x => (string)x.GetType().GetField("suspectId").GetValue(x) == suspect);
            Call(tour, "OpenObservation", binding.GetComponent("TrainObservation"));
            Assert.That((bool)Call(controller, "OfferConversation", State.Alibi(suspect)), Is.True); Assert.That(Sequence, Is.Not.Null); Finish();
        }
        Assert.That(State.progress.statements.Count, Is.EqualTo(7)); Assert.That(State.Has("M-S05"), Is.False);
        yield return null;
    }
    [UnityTest] public IEnumerator FullInvestigationConfrontationAndStaticEnding()
    {
        Finish(); foreach (var clue in State.catalogue.clues) State.Grant(clue.id);
        foreach (var suspect in new[] { "Felix", "Bellamy", "Mila" })
        {
            string interview = suspect == "Felix" ? "Felix — Champagne alibi" : suspect == "Mila" ? "Mila — Solitary rehearsal" : "Bellamy — Corridor witness";
            Call(controller, "Begin", interview, null); Finish();
        }
        foreach (var input in new[] { new[] { "C02", "M-S03", "Mila" }, new[] { "C07", "M-S04", "Mila" }, new[] { "C09", "F-S01", "Felix" } })
        {
            var theory = State.Construct(input[0], input[1]); Approach(input[2]);
            Assert.That((bool)Call(controller, "Confront", theory.id, input[2]), Is.True);
            Assert.That(Sequence, Is.Null); Assert.That((bool)Call(controller, "ConfirmConversation"), Is.True); Finish();
        }
        Assert.That(State.Has("M-S05"), Is.True); Assert.That(State.Has("F-S04"), Is.True);
        var ids = new[] { State.Construct("C03", "B-S01").id, State.Construct("C04", "F-S04").id, State.Construct("C10", "M-S05").id };
        Assert.That((bool)Call(controller, "Accuse", "Mila", ids), Is.False);
        while (State.InvestigationOpen) { Call(controller, "RequestEndDay"); Assert.That((bool)Call(controller, "ConfirmEndDay"), Is.True); }
        Assert.That((bool)Call(controller, "Accuse", "Felix", ids), Is.False);
        Assert.That((bool)Call(controller, "Accuse", "Mila", ids), Is.True);
        Assert.That(State.Has("flag:resolved"), Is.False); Assert.That(Sequence, Is.EqualTo("ENDING — Mila's account"));
        Finish(); Assert.That(State.Has("flag:resolved"), Is.True); yield return null;
    }
    [UnityTest] public IEnumerator InterviewMustFinishAndCannotBeReinitialized()
    {
        Finish(); Call(controller, "Begin", "Mila — Solitary rehearsal", null);
        Call(controller, "Advance"); Call(controller, "CancelDialogue");
        Assert.That(State.progress.statements, Is.Empty); Assert.That(Sequence, Is.Not.Null); Finish();
        Assert.That(State.Alibi("Mila"), Is.Null); yield return null;
    }
    [UnityTest] public IEnumerator RecordedBeatUsesMatchingIdAndWaitsForPlayback()
    {
        Finish(); var voice = State.catalogue.Voice("BURR_082");
        var clip = AudioClip.Create("Test silent recording", 4410, 1, 44100, false);
        voice.clip = clip;
        try
        {
            Call(controller, "DebugPlaySequence", "Reusable/Mila");
            Assert.That(controller.GetComponent<AudioSource>().clip, Is.SameAs(clip));
            Assert.That((string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller), Is.EqualTo(voice.id));
            Call(controller, "Advance");
            Assert.That((string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller), Is.EqualTo(voice.id));
            yield return new WaitForSecondsRealtime(.25f);
            Call(controller, "Advance");
            Assert.That((string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller), Is.EqualTo("MILA_REACT_QUESTION_01"));
        }
        finally { voice.clip = null; UnityEngine.Object.Destroy(clip); }
    }
    [UnityTest] public IEnumerator MissingRecordingOutsideDevelopmentModeStopsSafely()
    {
        Finish(); controller.GetType().GetField("developmentUnvoiced").SetValue(controller, false);
        LogAssert.Expect(LogType.Error, "Recording unavailable: BURR_082");
        Call(controller, "DebugPlaySequence", "Reusable/Mila");
        Assert.That(Sequence, Is.Null); Assert.That(State.Has("flag:resolved"), Is.False); yield return null;
    }
    void Approach(string suspect)
    {
        var actor = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).First(x => x.GetType().Name == "TrainActor" && (string)x.GetType().GetField("characterId").GetValue(x) == suspect);
        var detective = (CharacterController)tour.GetType().GetField("detective").GetValue(tour);
        detective.enabled = false; detective.transform.position = actor.transform.position + Vector3.back * 2; detective.enabled = true;
    }
    [UnityTest] public IEnumerator PaidConfirmationCanCancelAndCannotDoubleSpend()
    {
        Finish(); State.Grant("C01"); State.Grant("F-S01"); var t = State.Construct("C01", "F-S01"); Approach("Felix");
        Assert.That((bool)Call(controller, "Confront", t.id, "Felix"), Is.True); Assert.That(Sequence, Is.Null); Assert.That(State.RemainingActions, Is.EqualTo(3));
        Call(controller, "CancelConfirmation"); Assert.That(State.progress.usedInitializations, Is.Empty);
        Call(controller, "Confront", t.id, "Felix"); Assert.That((bool)Call(controller, "ConfirmConversation"), Is.True);
        Assert.That((bool)Call(controller, "ConfirmConversation"), Is.False); Assert.That(State.RemainingActions, Is.EqualTo(2));
        Call(controller, "CancelDialogue"); Assert.That(Sequence, Is.Not.Null); Finish();
        Assert.That(State.Presentation(t.id, "Felix"), Is.Null); yield return null;
    }
    [UnityTest] public IEnumerator NotebookCreatesConnectedGraphAndMapMarkerStaysBetweenStops()
    {
        Finish(); State.Grant("C01", "odette"); State.Grant("F-S01", "bar");
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Call(notebook, "SelectNode", "C01"); Call(notebook, "SelectNode", "F-S01"); yield return null; yield return null;
        var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
        Assert.That(graph.nodes.Count, Is.EqualTo(3)); Assert.That(State.progress.theories.Count, Is.EqualTo(1));
        Assert.That(graph.nodes.Single(n => n.kind == ReasoningNodeKind.Theory).clueParent, Is.EqualTo("C01"));
        Call(notebook, "SelectNode", "F-S01"); yield return null; Assert.That(State.progress.theories.Count, Is.EqualTo(1));
        var tabType = notebook.GetType().GetMethod("SelectTab").GetParameters()[0].ParameterType;
        Call(notebook, "SelectTab", Enum.ToObject(tabType, 0)); yield return null; yield return null; Canvas.ForceUpdateCanvases();
        var marker = (Vector2)notebook.GetType().GetProperty("JourneyMarker").GetValue(notebook);
        Assert.That(State.configuration.stops.All(s => Vector2.Distance(s.position, marker) > .001f), Is.True);
        Assert.That((int)notebook.GetType().GetProperty("JourneyConnection").GetValue(notebook), Is.Zero);
        Call(notebook, "SelectTab", Enum.ToObject(tabType, 2)); yield return null; yield return null;
        Assert.That(State.progress.discoveries.Single(d => d.id == "C01").locationId, Is.EqualTo("odette"));
        Assert.That(State.progress.discoveries.Single(d => d.id == "F-S01").locationId, Is.EqualTo("bar"));
    }
    [UnityTest] public IEnumerator FinalDayClosureLocksDiscoveriesAndInterviewPrompts()
    {
        Finish(); while (State.InvestigationOpen) { Call(controller, "RequestEndDay"); Call(controller, "ConfirmEndDay"); }
        Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Accusation));
        var clue = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(x => x.GetType().Name == "NarrativeInteraction" && (string)x.GetType().GetField("clueId").GetValue(x) == "C01");
        Call(tour, "OpenObservation", clue.GetComponent("TrainObservation")); Assert.That(State.progress.clues, Is.Empty);
        Assert.That(State.Alibi("Mila"), Is.Null); Assert.That(State.Construct("C01", "F-S01"), Is.Null); yield return null;
    }
    [UnityTest] public IEnumerator DialogueShowsLogbookCueOnlyForNewDiscoveries()
    {
        Finish(); Call(controller, "Begin", "Mila — Solitary rehearsal", null);
        int before = (int)controller.GetType().GetProperty("LogbookNoticeVersion").GetValue(controller);
        Call(controller, "Advance"); Assert.That((int)controller.GetType().GetProperty("LogbookNoticeVersion").GetValue(controller), Is.EqualTo(before));
        Call(controller, "Advance");
        Assert.That((string)controller.GetType().GetProperty("LogbookNotice").GetValue(controller), Does.Contain("NOTED IN LOGBOOK").And.Contain("Mila"));
        Assert.That((int)controller.GetType().GetProperty("LogbookNoticeVersion").GetValue(controller), Is.EqualTo(before + 1));
        Finish(); int known = (int)controller.GetType().GetProperty("LogbookNoticeVersion").GetValue(controller);
        Call(controller, "DebugPlaySequence", "Mila — Solitary rehearsal"); Finish();
        Assert.That((int)controller.GetType().GetProperty("LogbookNoticeVersion").GetValue(controller), Is.EqualTo(known)); yield return null;
    }
    [UnityTest] public IEnumerator NotebookRemovalClearsNodeAndCanBeRestored()
    {
        Finish(); State.Grant("C01"); State.Grant("F-S01"); var t = State.Construct("C01", "F-S01");
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Call(notebook, "SelectNode", t.id); yield return null; Assert.That((bool)Call(notebook, "RemoveSelectedTheory"), Is.True); yield return null; yield return null;
        var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
        Assert.That(graph.nodes.Any(n => n.id == t.id), Is.False); Assert.That(graph.nodes.Count, Is.EqualTo(2));
        Assert.That(State.RestoreTheory(t.id), Is.True); yield return null; yield return null;
        graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook); Assert.That(graph.nodes.Count, Is.EqualTo(3));
    }
    [UnityTest] public IEnumerator FinalPhaseGraphConstructsNewTheoriesAndTethersASupportedAccusation()
    {
        Finish(); foreach (var c in State.catalogue.clues) State.Grant(c.id); foreach (var s in State.catalogue.statements) State.Grant(s.id);
        int discoveries = State.progress.discoveries.Count;
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Assert.That((bool)notebook.GetType().GetProperty("AccusationGraphActive").GetValue(notebook), Is.True);
        Assert.That((bool)Call(notebook, "TetherAccusationToSuspect", "Mila"), Is.False);
        foreach (var pair in new[] { new[] { "C03", "B-S01" }, new[] { "C09", "F-S04" }, new[] { "C10", "M-S05" } })
        {
            Call(notebook, "SelectNode", pair[0]); Call(notebook, "SelectNode", pair[1]); yield return null; yield return null;
            Assert.That(State.progress.theories, Does.Contain(State.catalogue.Pair(pair[0], pair[1]).id));
            Call(notebook, "SelectNode", AccusationGraphLayout.DraftId); yield return null; yield return null;
        }
        Assert.That((bool)notebook.GetType().GetProperty("CanSubmitAccusation").GetValue(notebook), Is.False);
        Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); yield return null; yield return null;
        Assert.That((string)notebook.GetType().GetProperty("AccusationSuspect").GetValue(notebook), Is.EqualTo("Mila"));
        var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
        Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Suspect), Is.EqualTo(3));
        var faded = notebook.GetComponentsInChildren<MonoBehaviour>().First(c => c.GetType().Name == "NotebookNodeGraphic" && c.gameObject.name == "C03");
        Assert.That(((Color)faded.GetType().GetProperty("color").GetValue(faded)).a, Is.LessThan(.4f));
        Assert.That(State.progress.discoveries.Count, Is.EqualTo(discoveries)); Assert.That(State.progress.actionsSpent, Is.Zero);
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.True); Finish(); Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Resolved));
    }
    [UnityTest] public IEnumerator AccusationTethersEnforceThreeAndPruneRemovedTheoriesWithoutReopeningInvestigation()
    {
        Finish(); foreach (var c in State.catalogue.clues) State.Grant(c.id); foreach (var s in State.catalogue.statements) State.Grant(s.id);
        var ids = new[] { State.Construct("C03", "B-S01").id, State.Construct("C09", "F-S04").id, State.Construct("C10", "M-S05").id, State.Construct("C01", "F-S01").id };
        var notebook = controller.GetComponent("InvestigationNotebook"); Assert.That((bool)Call(notebook, "BeginAccusationGraph"), Is.False);
        while (State.InvestigationOpen) State.EndDay(out _);
        Call(controller, "ToggleNotebook"); yield return null; yield return null;
        foreach (var id in ids.Take(3)) Assert.That((bool)Call(notebook, "TetherTheoryToAccusation", id), Is.True);
        Assert.That((bool)Call(notebook, "TetherTheoryToAccusation", ids[3]), Is.False);
        Assert.That((bool)Call(notebook, "TetherAccusationToSuspect", "Mila"), Is.True);
        State.RemoveTheory(ids[0]); yield return null; yield return null;
        Assert.That((bool)notebook.GetType().GetProperty("CanSubmitAccusation").GetValue(notebook), Is.False);
        Assert.That(notebook.GetType().GetProperty("AccusationSuspect").GetValue(notebook), Is.Null);
        State.RestoreTheory(ids[0]); yield return null;
        Assert.That((bool)Call(notebook, "TetherTheoryToAccusation", ids[0]), Is.True);
        Assert.That((bool)Call(notebook, "TetherAccusationToSuspect", "Felix"), Is.True);
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.False); Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Accusation));
        Assert.That(State.Construct("C02", "F-S01"), Is.Not.Null); yield return null;
    }
    [UnityTest] public IEnumerator DialogueClueRewardAlsoShowsLogbookCue()
    {
        Finish(); var beat = State.catalogue.Sequence("Felix — Champagne alibi").beats[3];
        bool alreadyPresent = beat.grants.Contains("C05"); if (!alreadyPresent) beat.grants.Add("C05");
        try
        {
            Call(controller, "Begin", "Felix — Champagne alibi", null);
            for (int i = 0; i < 4; i++) Call(controller, "Advance");
            Assert.That(State.Has("C05"), Is.True);
            Assert.That((string)controller.GetType().GetProperty("LogbookNotice").GetValue(controller), Does.Contain("Clue: Champagne receipt"));
        }
        finally { if (!alreadyPresent) beat.grants.Remove("C05"); }
        yield return null;
    }
}
