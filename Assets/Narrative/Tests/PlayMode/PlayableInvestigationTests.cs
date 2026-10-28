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
        for (int i = 0; i < 100 && Sequence != null; i++)
        {
            if ((bool)controller.GetType().GetProperty("AwaitingIntuition").GetValue(controller))
            {
                string entry = (string)controller.GetType().GetProperty("IntuitionEntry").GetValue(controller);
                Call(controller, "ChooseIntuition", State.catalogue.InterpretationTags(entry).First(AccusationReasoning.Roles.Contains));
            }
            else Call(controller, "Advance");
        }
        Assert.That(Sequence, Is.Null);
    }
    IEnumerator WaitForAutomaticDefense(Component view)
    {
        var defense = (DoubtDefense)view.GetType().GetProperty("Defense").GetValue(view);
        Assert.That(defense.Draft.started, Is.True);
        float deadline = Time.realtimeSinceStartup + 12;
        while (!defense.Draft.finished && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.That(defense.Draft.finished, Is.True, "The run must reach an outcome without manual stepping.");
    }
    UnityEngine.UI.Button ViewButton(Component view, string name) => view.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == name);
    [UnityTest] public IEnumerator CoffeeStandConnectsReachableExplorationToPersistentFocusInBothScenes()
    {
        foreach (var scene in new[] { "CarnivalExpress", "ThirdPersonTrain" })
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null;
            controller = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c => c.GetType().Name == "TrainNarrative"); tour = controller.GetComponent("TrainTour"); Finish();
            var stand = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Single(c => c.GetType().Name == "FocusCoffeeStand");
            int before = State.FocusCapacity; var observation = stand.GetComponent("TrainObservation");
            Call(tour, "OpenObservation", observation); Assert.That(State.FocusCapacity, Is.EqualTo(before), "Distant coffee cannot be collected.");
            Call(tour, "SetView", 2, true); ApproachClue(stand, -1.5f); Call(tour, "SnapCamera"); yield return null; yield return null;
            Assert.That((bool)stand.GetType().GetProperty("MarkerVisible").GetValue(stand), Is.True);
            yield return CaptureInteractionReview("coffee-focus-" + scene);
            Call(tour, "OpenObservation", observation); yield return null;
            Assert.That(State.FocusCapacity, Is.EqualTo(before + 2)); Assert.That(State.progress.bonusFocus, Is.EqualTo(2));
            Assert.That(State.progress.clues, Is.Empty); Assert.That(State.progress.actionsSpent, Is.Zero);
            Assert.That((bool)stand.GetType().GetProperty("MarkerVisible").GetValue(stand), Is.False);
            Call(tour, "CloseObservation"); Call(tour, "OpenObservation", observation); Assert.That(State.FocusCapacity, Is.EqualTo(before + 2));
            var restored = new InvestigationState(State.catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(State.progress)), State.configuration);
            Assert.That(restored.FocusCapacity, Is.EqualTo(before + 2)); Assert.That(restored.ClaimCoffeeFocus(), Is.False);
            Call(tour, "CloseObservation"); foreach (var id in new[] { "C02", "M-S04", "C10" }) State.Grant(id); State.Construct("C02", "M-S04");
            Call(controller, "ToggleNotebook"); yield return null; yield return null; var notebook = controller.GetComponent("InvestigationNotebook"); Call(notebook, "SelectNode", "clock:position"); yield return null;
            yield return CaptureInteractionReview("coffee-focus-board-" + scene, 1280, 900);
            Assert.That(controller.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.StartsWith("Focus 0 / " + (before + 2))), Is.True);
        }
    }
    void CollectClockDoubts(bool all)
    {
        foreach (var id in new[] { "C02", "M-S04", "C10" }) State.Grant(id);
        if (all) { State.Grant("M-S05"); State.Grant("C07"); }
        State.Construct("C02", "M-S04"); State.configuration.doubtTuning.stepSeconds = .04f;
    }
    void PlaceClockFormation(Component view, bool extraShot)
    {
        Call(view, "SelectEntry", "C02"); Call(view, "PlaceSelected", 6f, 6f);
        Call(view, "SelectEntry", "C10"); Call(view, "PlaceSelected", 10f, 6f);
        Call(view, "SelectEntry", "M-S04"); Call(view, "PlaceSelected", 8f, 3f);
        Call(view, "SelectEntry", "T_C02_MS04"); Call(view, "PlaceSelected", 8f, 9f);
        if (extraShot) { Call(view, "SelectEntry", "C07"); Call(view, "PlaceSelected", 8f, 4f); }
    }
    [UnityTest] public IEnumerator AuthoredContradictionsCreateMidpointConnectionsAndOneAccumulatedDefenseInBothScenes()
    {
        foreach (var scene in new[] { "CarnivalExpress", "ThirdPersonTrain" })
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null;
            controller = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c => c.GetType().Name == "TrainNarrative"); tour = controller.GetComponent("TrainTour"); Finish();
            foreach (var id in new[] { "C02", "M-S04", "C10", "C07", "M-S05" }) State.Grant(id);
            Call(controller, "ToggleNotebook"); yield return null; yield return null;
            var notebook = controller.GetComponent("InvestigationNotebook");
            var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
            Assert.That(graph.nodes.Any(n => n.kind == ReasoningNodeKind.Doubt), Is.False);
            var question = State.Construct("C02", "M-S04"); yield return null; yield return null;
            graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
            Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Doubt), Is.EqualTo(3));
            var doubt = graph.nodes.Single(n => n.id == "clock:position");
            Assert.That(doubt.questionParent, Is.EqualTo(question.id)); Assert.That(doubt.doubtSource, Is.EqualTo("C10"));
            Assert.That(doubt.position, Is.EqualTo((graph.nodes.Single(n => n.id == question.id).position + graph.nodes.Single(n => n.id == "C10").position) * .5f));
            Assert.That((Vector2)Call(notebook, "NodePosition", doubt.id), Is.EqualTo(((Vector2)Call(notebook, "NodePosition", question.id) + (Vector2)Call(notebook, "NodePosition", "C10")) * .5f));
            Assert.That(State.DoubtPressure, Is.EqualTo(3)); Assert.That(State.Presentation(question.id, "Felix"), Is.Null);
            yield return CaptureInteractionReview("authored-doubt-graph-" + scene, 1280, 900);
            Call(notebook, "SelectNode", doubt.id); yield return null;
            var view = controller.GetComponent("DoubtDefenseView"); PlaceClockFormation(view, true); yield return null;
            var defense = (DoubtDefense)view.GetType().GetProperty("Defense").GetValue(view);
            Assert.That(defense.Interpretation, Is.EqualTo(question.text)); Assert.That(defense.Draft.waves, Is.EqualTo(2)); Assert.That(defense.Draft.environment.Count, Is.EqualTo(3));
            yield return CaptureInteractionReview("authored-doubt-planning-" + scene, 1280, 900);
            yield return CaptureInteractionReview("authored-doubt-planning-narrow-" + scene);
            State.configuration.doubtTuning.stepSeconds = .3f; ViewButton(view, "Test defense").onClick.Invoke(); yield return null;
            float deadline = Time.realtimeSinceStartup + 3; while (defense.Draft.tick < 3 && Time.realtimeSinceStartup < deadline) yield return null;
            yield return CaptureInteractionReview("authored-doubt-running-" + scene, 1280, 900);
            State.configuration.doubtTuning.stepSeconds = .04f; yield return WaitForAutomaticDefense(view);
            Assert.That(defense.Draft.defended, Is.True); Assert.That(State.DoubtPressure, Is.Zero); Assert.That(State.QuestionSecure(question.id), Is.True);
            yield return CaptureInteractionReview("authored-doubt-success-" + scene, 1280, 900);
            Assert.That(view.GetComponentsInChildren<UnityEngine.UI.InputField>().Any(i => i.name == "Reflection note"), Is.False);
            Assert.That(view.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b => b.name == "Reflect"), Is.False);
            ViewButton(view, "Back to reasoning").onClick.Invoke(); yield return null; yield return null;
            Assert.That((bool)view.GetType().GetProperty("IsOpen").GetValue(view), Is.False);
            graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
            Assert.That(graph.nodes.Any(n => n.kind == ReasoningNodeKind.Doubt), Is.False, "One defense clears the accumulated connections.");
            var questionNode = notebook.GetComponentsInChildren<MonoBehaviour>().Single(c => c.GetType().Name == "NotebookNodeGraphic" && c.name == question.id);
            Assert.That(questionNode.transform.Find("Doubt resolved badge"), Is.Not.Null);
            Call(notebook, "SelectNode", question.id); yield return null; yield return null;
            var nav = notebook.GetComponentsInChildren<MonoBehaviour>().Single(c => c.GetType().Name == "NotebookPanZoom");
            var content = (RectTransform)nav.GetType().GetField("content").GetValue(nav); nav.GetType().GetField("zoom").SetValue(nav, 1f);
            content.localScale = Vector3.one; content.anchoredPosition = -(Vector2)Call(notebook, "NodePosition", question.id);
            yield return CaptureInteractionReview("resolved-question-graph-" + scene, 1280, 900);
            Assert.That(State.progress.theories, Does.Contain(question.id)); Assert.That(State.progress.actionsSpent, Is.Zero);
        }
    }
    [UnityTest] public IEnumerator InsecureQuestionIsDisabledUntilSuccessfulDefenseAndFailureDoesNotUnlock()
    {
        Finish(); CollectClockDoubts(false); Approach("Felix"); Call(controller, "OpenQuestionMenu", "Felix"); yield return null;
        var menu = controller.GetComponent("SuspectQuestionMenu"); var locked = State.AvailableConversations("Felix").Single(p => p.theoryId == "T_C02_MS04");
        Assert.That(menu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == locked.key).interactable, Is.False);
        Assert.That((bool)Call(menu, "Choose", locked.key), Is.False); Assert.That(State.progress.reservation, Is.Null);
        yield return CaptureInteractionReview("authored-doubt-locked-interview", 1280, 900);
        Call(menu, "Close"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(notebook, "SelectNode", "clock:position"); yield return null;
        var view = controller.GetComponent("DoubtDefenseView");
        State.configuration.doubtTuning.invaderHealth = 10000;
        Call(view, "SelectEntry", "C02"); Call(view, "PlaceSelected", 1f, 2f); ViewButton(view, "Test defense").onClick.Invoke(); yield return null;
        yield return WaitForAutomaticDefense(view);
        var defense = (DoubtDefense)view.GetType().GetProperty("Defense").GetValue(view);
        Assert.That(defense.Draft.defended, Is.False); Assert.That(State.QuestionSecure("T_C02_MS04"), Is.False); Assert.That(State.DoubtPressure, Is.EqualTo(1));
        ViewButton(view, "Rearrange").onClick.Invoke(); State.configuration.doubtTuning.invaderHealth = 8; PlaceClockFormation(view, false); yield return null;
        ViewButton(view, "Test defense").onClick.Invoke(); yield return null; yield return WaitForAutomaticDefense(view);
        Assert.That(defense.Draft.defended, Is.True); Assert.That(State.QuestionSecure("T_C02_MS04"), Is.True);
        var restored = new InvestigationState(State.catalogue, JsonUtility.FromJson<InvestigationProgress>(JsonUtility.ToJson(State.progress)), State.configuration);
        Assert.That(restored.DoubtPressure, Is.Zero); Assert.That(restored.Presentation("T_C02_MS04", "Felix"), Is.Not.Null);
        Call(view, "Close"); Call(controller, "ToggleNotebook"); Approach("Felix"); Call(controller, "OpenQuestionMenu", "Felix"); yield return null;
        var ready = State.AvailableConversations("Felix").Single(p => p.theoryId == "T_C02_MS04");
        Assert.That(menu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.name == ready.key).interactable, Is.True);
        Assert.That(menu.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.Contains("Doubt resolved")), Is.True);
        yield return CaptureInteractionReview("resolved-question-dialogue-menu", 1280, 900);
        Assert.That((bool)Call(menu, "Choose", ready.key), Is.True);
        Assert.That((bool)controller.GetType().GetProperty("CurrentQuestionDoubtResolved").GetValue(controller), Is.True);
        Finish(); Assert.That(State.progress.actionsSpent, Is.Zero);
        State.Grant("M-S05"); Assert.That(State.QuestionDoubtResolved("T_C02_MS04"), Is.False);
        Call(controller, "ToggleNotebook"); yield return null; yield return null;
        var node = notebook.GetComponentsInChildren<MonoBehaviour>().Single(c => c.GetType().Name == "NotebookNodeGraphic" && c.name == "T_C02_MS04");
        Assert.That(node.transform.Find("Doubt resolved badge"), Is.Null);
    }
    [UnityTest] public IEnumerator OptionalAccusationReviewCanRevisitAnAuthoredDoubtWithoutForcingAnAccusationDefense()
    {
        Finish(); foreach (var id in new[] { "C06", "F-S04", "C04", "M-S01" }) State.Grant(id); State.Construct("C04", "F-S04");
        Call(controller, "ToggleNotebook"); var notebook = controller.GetComponent("InvestigationNotebook"); Call(notebook, "BeginAccusationGraph"); yield return null;
        Call(notebook, "SelectNode", "C06"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Motive));
        Call(notebook, "SelectNode", "F-S04"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Opportunity));
        Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); Assert.That((bool)Call(notebook, "RequestAccusationReview"), Is.True); yield return null;
        var view = controller.GetComponent("DoubtDefenseView"); ViewButton(view, "Revisit relevant doubt").onClick.Invoke(); yield return null;
        Assert.That(((DoubtDefense)view.GetType().GetProperty("Defense").GetValue(view)).Definition.sourceId, Is.EqualTo("M-S01"));
        Call(view, "Close"); yield return null; ViewButton(view, "Proceed with uncertainty").onClick.Invoke();
        Assert.That(Sequence, Is.EqualTo("ENDING — Mila's account")); Finish(); Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Resolved));
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
    [UnityTest] public IEnumerator WrongIntuitionPausesForCorrectionAndChargesOnlyOnce()
    {
        Finish(); var pacing = (InvestigationConfiguration)controller.GetType().GetField("configuration").GetValue(controller);
        pacing.actionCostsEnabled = true; pacing.intuitionMistakeCost = 2; State.RefreshConfiguration();
        State.Grant("C09"); State.Grant("F-S01"); var question = State.Construct("C09", "F-S01"); Approach("Felix");
        Assert.That((bool)Call(controller, "Confront", question.id, "Felix"), Is.True);
        Assert.That((bool)Call(controller, "ConfirmConversation"), Is.True);
        for (int i = 0; i < 30 && !(bool)controller.GetType().GetProperty("AwaitingIntuition").GetValue(controller); i++) Call(controller, "Advance");
        Assert.That((string)controller.GetType().GetProperty("IntuitionEntry").GetValue(controller), Is.EqualTo("F-S04"));
        string voice = (string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller);
        Call(controller, "Advance"); Assert.That(State.Has("F-S04"), Is.False);
        Assert.That((bool)Call(controller, "ChooseIntuition", AccusationReasoning.Motive), Is.True);
        Assert.That(State.progress.actionsSpent, Is.EqualTo(2)); Assert.That(State.Has("F-S04"), Is.False);
        StringAssert.Contains("Opportunity", (string)controller.GetType().GetProperty("IntuitionFeedback").GetValue(controller));
        Assert.That((string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller), Is.EqualTo(voice));
        Assert.That((bool)Call(controller, "ChooseIntuition", AccusationReasoning.Opportunity), Is.False);
        yield return CaptureInteractionReview("intuition-correction");
        Finish(); Assert.That(State.Has("F-S04"), Is.True); Assert.That(State.progress.actionsSpent, Is.EqualTo(2));
    }
    [UnityTest] public IEnumerator CorrectIntuitionMovesOnAndRefundsConversation()
    {
        Finish(); var pacing = (InvestigationConfiguration)controller.GetType().GetField("configuration").GetValue(controller);
        pacing.actionCostsEnabled = true; State.RefreshConfiguration();
        State.Grant("C06"); State.Grant("M-S03"); var question = State.Construct("C06", "M-S03"); Approach("Mila");
        Call(controller, "Confront", question.id, "Mila"); Call(controller, "ConfirmConversation");
        for (int i = 0; i < 30 && !(bool)controller.GetType().GetProperty("AwaitingIntuition").GetValue(controller); i++) Call(controller, "Advance");
        Assert.That((string)controller.GetType().GetProperty("IntuitionEntry").GetValue(controller), Is.EqualTo("M-S06"));
        yield return CaptureInteractionReview("intuition-choice");
        Assert.That((bool)Call(controller, "ChooseIntuition", AccusationReasoning.Motive), Is.True);
        Assert.That(State.Has("M-S06"), Is.True); Assert.That(State.progress.actionsSpent, Is.Zero);
        Assert.That(State.FocusCapacity, Is.EqualTo(7)); StringAssert.Contains("+1 FOCUS", (string)controller.GetType().GetProperty("LogbookNotice").GetValue(controller));
        Finish(); Assert.That(State.progress.actionsSpent, Is.Zero);
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
            State.HintClue(clue, "Test setup", "fixture"); ApproachClue(binding);
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
        State.configuration.actionCostsEnabled = true;
        Finish(); foreach (var clue in State.catalogue.clues) State.Grant(clue.id);
        foreach (var suspect in new[] { "Felix", "Bellamy", "Mila" })
        {
            string interview = suspect == "Felix" ? "Felix — Champagne alibi" : suspect == "Mila" ? "Mila — Solitary rehearsal" : "Bellamy — Corridor witness";
            Approach(suspect); Call(controller, "Begin", interview, null); Finish();
        }
        foreach (var input in new[] { new[] { "C02", "M-S03", "Mila" }, new[] { "C07", "M-S04", "Mila" }, new[] { "C09", "F-S01", "Felix" } })
        {
            var theory = State.Construct(input[0], input[1]); Approach(input[2]);
            Assert.That((bool)Call(controller, "Confront", theory.id, input[2]), Is.True);
            Assert.That(Sequence, Is.Null); Assert.That((bool)Call(controller, "ConfirmConversation"), Is.True); Finish();
        }
        Assert.That(State.Has("M-S05"), Is.True); Assert.That(State.Has("F-S04"), Is.True);
        Call(controller, "RequestEndDay"); Call(controller, "ConfirmEndDay");
        var motiveLead = State.Construct("C06", "M-S03"); Approach("Mila");
        Assert.That((bool)Call(controller, "Confront", motiveLead.id, "Mila"), Is.True);
        Assert.That((bool)Call(controller, "ConfirmConversation"), Is.True); Finish();
        var ids = new[] { State.Construct("C04", "F-S04").id, State.Construct("C06", "M-S06").id };
        Assert.That((bool)Call(controller, "Accuse", "Felix", ids), Is.False);
        while (State.InvestigationOpen) { Call(controller, "RequestEndDay"); Assert.That((bool)Call(controller, "ConfirmEndDay"), Is.True); }
        Assert.That((bool)Call(controller, "Accuse", "Felix", ids), Is.False);
        Assert.That((bool)Call(controller, "Accuse", "Mila", ids), Is.True);
        Assert.That(State.Has("flag:resolved"), Is.False); Assert.That(Sequence, Is.EqualTo("ENDING — Mila's account"));
        Finish(); Assert.That(State.Has("flag:resolved"), Is.True); yield return null;
    }
    [UnityTest] public IEnumerator InterviewMustFinishAndCannotBeReinitialized()
    {
        Finish(); Approach("Mila"); Call(controller, "Begin", "Mila — Solitary rehearsal", null);
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
    void ApproachClue(Component binding, float distance = 1.8f)
    {
        var detective = (CharacterController)tour.GetType().GetField("detective").GetValue(tour);
        var point = binding.transform.position + Vector3.back * distance; point.y = 1.12f;
        detective.enabled = false; detective.transform.position = point; detective.enabled = true;
    }
    static IEnumerator CaptureInteractionReview(string name, int captureWidth = 0, int captureHeight = 0)
    {
        yield return null;
        var camera = Camera.main; Assert.That(camera, Is.Not.Null);
        int width = captureWidth > 0 ? captureWidth : Mathf.Max(1, Screen.width), height = captureHeight > 0 ? captureHeight : Mathf.Max(1, Screen.height);
        var target = new RenderTexture(width, height, 24);
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
        var overlays = UnityEngine.Object.FindObjectsByType<Canvas>().Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay)
            .Select(c => (canvas: c, worldCamera: c.worldCamera, distance: c.planeDistance)).ToList();
        try
        {
            // A dedicated render target also works in batch mode, where there is no Game-view backbuffer.
            camera.targetTexture = target;
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceCamera; item.canvas.worldCamera = camera;
                item.canvas.planeDistance = camera.nearClipPlane + .1f;
            }
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            var directory = System.IO.Path.GetFullPath("Documentation/Narrative/InteractionPreviews");
            System.IO.Directory.CreateDirectory(directory);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(directory, name + ".png"), pixels.EncodeToPNG());
        }
        finally
        {
            foreach (var item in overlays)
            {
                item.canvas.renderMode = RenderMode.ScreenSpaceOverlay; item.canvas.worldCamera = item.worldCamera; item.canvas.planeDistance = item.distance;
            }
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(pixels);
            Canvas.ForceUpdateCanvases();
        }
    }
    [UnityTest] public IEnumerator DialogueHintRevealsMarkerButRequiresPhysicalCollectionInBothScenes()
    {
        foreach (var scene in new[] { "CarnivalExpress", "ThirdPersonTrain" })
        {
            yield return SceneManager.LoadSceneAsync(scene); yield return null;
            controller = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c => c.GetType().Name == "TrainNarrative"); tour = controller.GetComponent("TrainTour"); Finish();
            var markers = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Where(c => c.GetType().Name == "ClueMarker").ToArray();
            Assert.That(markers.Length, Is.EqualTo(10));
            var clusterClue = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Single(c => c.GetType().Name == "NarrativeInteraction" && (string)c.GetType().GetField("clueId").GetValue(c) == "C03");
            var clusterViews = (Transform[])tour.GetType().GetField("roomViews").GetValue(tour);
            int clusterRoom = Enumerable.Range(0, clusterViews.Length).OrderBy(i => Mathf.Abs(clusterViews[i].position.x - clusterClue.transform.position.x)).First();
            Call(tour, "SetView", clusterRoom, true); Call(tour, "SnapCamera"); yield return null; yield return null;
            var visibleMarkers = markers.Where(m => (bool)m.GetType().GetProperty("Visible").GetValue(m)).ToArray();
            Assert.That(visibleMarkers.Length, Is.GreaterThanOrEqualTo(3));
            for (int i = 0; i < visibleMarkers.Length; i++) for (int j = i + 1; j < visibleMarkers.Length; j++)
                Assert.That(((Rect)visibleMarkers[i].GetType().GetProperty("ScreenBounds").GetValue(visibleMarkers[i])).Overlaps((Rect)visibleMarkers[j].GetType().GetProperty("ScreenBounds").GetValue(visibleMarkers[j])), Is.False);
            yield return CaptureInteractionReview("clustered-clue-markers-" + scene);
            var binding = UnityEngine.Object.FindObjectsByType<MonoBehaviour>().Single(c => c.GetType().Name == "NarrativeInteraction" && (string)c.GetType().GetField("clueId").GetValue(c) == "C05");
            var marker = binding.GetComponent("ClueMarker");
            Assert.That((bool)marker.GetType().GetProperty("Revealed").GetValue(marker), Is.False);
            ApproachClue(binding); Call(tour, "OpenObservation", binding.GetComponent("TrainObservation")); Assert.That(State.Has("C05"), Is.False);
            Approach("Felix"); Assert.That((bool)Call(controller, "OpenQuestionMenu", "Felix"), Is.True); yield return null; yield return null;
            var menu = controller.GetComponent("SuspectQuestionMenu");
            var options = (System.Collections.Generic.IEnumerable<ConversationInitialization>)menu.GetType().GetProperty("Options").GetValue(menu);
            var alibi = options.Single(p => p.kind == ConversationKind.Alibi);
            yield return CaptureInteractionReview("suspect-alibi-menu-" + scene);
            Assert.That((bool)Call(menu, "Choose", alibi.key), Is.True);
            for (int i = 0; i < 20 && (string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller) != "FELI_002"; i++) Call(controller, "Advance");
            Assert.That((string)controller.GetType().GetProperty("CurrentVoiceId").GetValue(controller), Is.EqualTo("FELI_002"));
            Assert.That(State.HasClueHint("C05"), Is.False);
            Call(controller, "Advance"); Assert.That(State.HasClueHint("C05"), Is.True); Assert.That(State.Has("C05"), Is.False);
            Assert.That((bool)marker.GetType().GetProperty("Revealed").GetValue(marker), Is.True); Finish();
            var views = (Transform[])tour.GetType().GetField("roomViews").GetValue(tour);
            int room = Enumerable.Range(0, views.Length).OrderBy(i => Mathf.Abs(views[i].position.x - binding.transform.position.x)).First();
            Call(tour, "SetView", room, true); ApproachClue(binding); Call(tour, "SnapCamera"); yield return null; yield return null;
            var markerRoot = GameObject.Find("World clue marker / C05"); Assert.That(markerRoot, Is.Not.Null);
            var inspectButton = markerRoot.GetComponentInChildren<UnityEngine.UI.Button>(); Assert.That(inspectButton.interactable, Is.True);
            yield return CaptureInteractionReview("clue-marker-" + scene);
            Call(controller, "ToggleNotebook"); yield return null; yield return null;
            var notebook = controller.GetComponent("InvestigationNotebook");
            var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
            Assert.That(graph.nodes.Single(n => n.id == "C05").anticipated, Is.True); yield return CaptureInteractionReview("anticipated-clue-" + scene);
            var icon = notebook.GetComponentsInChildren<MonoBehaviour>().Single(c => c.GetType().Name == "NotebookNodeGraphic" && c.name == "C05");
            var color = (Color)icon.GetType().GetProperty("color").GetValue(icon); Assert.That(color.r, Is.EqualTo(color.g).Within(.05f));
            Call(notebook, "SelectNode", "C05"); Call(notebook, "SelectNode", "F-S01"); Assert.That(State.progress.theories, Is.Empty);
            Call(controller, "ToggleNotebook"); ApproachClue(binding, 12);
            Call(tour, "OpenObservation", binding.GetComponent("TrainObservation")); Call(controller, "Interact", binding.GetComponent("TrainObservation"));
            Assert.That(State.Has("C05"), Is.False); Assert.That((bool)marker.GetType().GetProperty("Interactable").GetValue(marker), Is.False);
            ApproachClue(binding); yield return null; yield return null;
            Assert.That((bool)marker.GetType().GetProperty("Interactable").GetValue(marker), Is.True);
            Assert.That(inspectButton.interactable, Is.True); inspectButton.onClick.Invoke(); Assert.That(State.Has("C05"), Is.True);
            Assert.That((bool)marker.GetType().GetProperty("Revealed").GetValue(marker), Is.False);
            Assert.That(State.progress.discoveries.Single(d => d.id == "C05").locationId, Is.Not.EqualTo("unknown"));
            Assert.That(ReasoningGraphLayout.Build(State.catalogue, State.progress).nodes.Single(n => n.id == "C05").anticipated, Is.False);
        }
    }
    [UnityTest] public IEnumerator PaidConfirmationCanCancelAndCannotDoubleSpend()
    {
        State.configuration.actionCostsEnabled = true;
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
        Assert.That(graph.nodes.Any(n => n.kind == ReasoningNodeKind.Doubt), Is.False);
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
        Finish(); Approach("Mila"); Call(controller, "Begin", "Mila — Solitary rehearsal", null);
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
        Assert.That(graph.nodes.Any(n => n.kind == ReasoningNodeKind.Doubt), Is.False);
    }
    [UnityTest] public IEnumerator FinalPhaseGraphConstructsNewTheoriesAndTethersASupportedAccusation()
    {
        Finish(); foreach (var c in State.catalogue.clues) State.Grant(c.id); foreach (var s in State.catalogue.statements) State.Grant(s.id);
        int discoveries = State.progress.discoveries.Count;
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Assert.That((bool)notebook.GetType().GetProperty("AccusationGraphActive").GetValue(notebook), Is.True);
        Assert.That((bool)Call(notebook, "TetherAccusationToSuspect", "Mila"), Is.False);
        foreach (var pair in new[] { new[] { "C09", "F-S04" }, new[] { "C06", "M-S06" } })
        {
            var question = State.Construct(pair[0], pair[1]); yield return null; yield return null; Call(notebook, "SelectNode", question.id); yield return null; yield return null;
            Assert.That(State.progress.theories, Does.Contain(State.catalogue.Pair(pair[0], pair[1]).id));
            Call(notebook, "SelectNode", AccusationGraphLayout.DraftId); yield return null; yield return null;
        }
        Assert.That((bool)notebook.GetType().GetProperty("CanSubmitAccusation").GetValue(notebook), Is.False);
        Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); yield return null; yield return null;
        Assert.That((string)notebook.GetType().GetProperty("AccusationSuspect").GetValue(notebook), Is.EqualTo("Mila"));
        var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
        Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.Suspect), Is.EqualTo(3));
        var faded = notebook.GetComponentsInChildren<MonoBehaviour>().First(c => c.GetType().Name == "NotebookNodeGraphic" && c.gameObject.name == "C03");
        Assert.That(((Color)faded.GetType().GetProperty("color").GetValue(faded)).a, Is.EqualTo(1f));
        Assert.That(State.progress.discoveries.Count, Is.EqualTo(discoveries)); Assert.That(State.progress.actionsSpent, Is.Zero);
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.True); Finish(); Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Resolved));
    }
    [UnityTest] public IEnumerator AccusationTethersEnforceTwoAndPruneRemovedTheoriesWithoutReopeningInvestigation()
    {
        Finish(); foreach (var c in State.catalogue.clues) State.Grant(c.id); foreach (var s in State.catalogue.statements) State.Grant(s.id);
        var ids = new[] { State.Construct("C03", "B-S01").id, State.Construct("C09", "F-S04").id, State.Construct("C06", "M-S06").id, State.Construct("C01", "F-S01").id };
        var notebook = controller.GetComponent("InvestigationNotebook"); Assert.That((bool)Call(notebook, "BeginAccusationGraph"), Is.True);
        while (State.InvestigationOpen) State.EndDay(out _);
        Call(controller, "ToggleNotebook"); yield return null; yield return null;
        foreach (var id in ids.Take(2)) Assert.That((bool)Call(notebook, "TetherTheoryToAccusation", id), Is.True);
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
    [UnityTest] public IEnumerator SuspectMenuOffersUnaskedAlibiAndMatchingQuestionIcons()
    {
        Finish(); State.Grant("C06"); State.Grant("F-S02"); var question = State.Construct("C06", "F-S02"); Approach("Felix");
        Assert.That((bool)Call(controller, "OpenQuestionMenu", "Felix"), Is.True); yield return null; yield return null;
        var menu = controller.GetComponent("SuspectQuestionMenu");
        Assert.That((bool)controller.GetType().GetProperty("NotebookOpen").GetValue(controller), Is.False);
        var options = (System.Collections.Generic.IEnumerable<ConversationInitialization>)menu.GetType().GetProperty("Options").GetValue(menu);
        Assert.That(options.Any(p => p.kind == ConversationKind.Alibi), Is.True);
        Assert.That(options.Any(p => p.theoryId == question.id), Is.True);
        var canvas = controller.GetComponentsInChildren<Canvas>().Single(c => c.name == "Suspect questioning menu"); yield return CaptureInteractionReview("suspect-question-menu");
        Assert.That(canvas.GetComponentsInChildren<MonoBehaviour>().Any(c => c.GetType().Name == "NotebookNodeGraphic" && (ReasoningNodeKind)c.GetType().GetField("kind").GetValue(c) == ReasoningNodeKind.Theory), Is.True);
        Assert.That(canvas.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t => t.text.StartsWith(ReasoningGraphLayout.QuestionLabel(State.progress, question.id))), Is.True);
        var alibi = options.Single(p => p.kind == ConversationKind.Alibi);
        Assert.That((bool)Call(menu, "Choose", alibi.key), Is.True); Finish();
        Call(controller, "OpenQuestionMenu", "Felix"); yield return null;
        options = (System.Collections.Generic.IEnumerable<ConversationInitialization>)menu.GetType().GetProperty("Options").GetValue(menu);
        Assert.That(options.Any(p => p.kind == ConversationKind.Alibi), Is.False);
        var offered = options.Single(p => p.theoryId == question.id);
        State.configuration.actionCostsEnabled = true; State.progress.actionsSpent = State.ActionsPerDay; State.RefreshConfiguration(); yield return null;
        Assert.That((bool)Call(menu, "Choose", offered.key), Is.False); Assert.That(Sequence, Is.Null);
        State.progress.actionsSpent = 0; State.RefreshConfiguration();
        Assert.That((bool)Call(menu, "Choose", offered.key), Is.True); Assert.That(Sequence, Is.Null);
        Call(controller, "CancelConfirmation"); Assert.That(State.Presentation(question.id, "Felix"), Is.Not.Null);
        State.configuration.actionCostsEnabled = false; Call(controller, "OpenQuestionMenu", "Felix"); yield return null;
        Assert.That((bool)Call(menu, "Choose", offered.key), Is.True); Finish();
        Call(controller, "OpenQuestionMenu", "Felix"); yield return null;
        options = (System.Collections.Generic.IEnumerable<ConversationInitialization>)menu.GetType().GetProperty("Options").GetValue(menu);
        Assert.That(options.Any(p => p.theoryId == question.id), Is.False);
    }
    [UnityTest] public IEnumerator AccusationChecklistShowsTheReportedGapAndUpdatesWhenTheTheoryIsReplaced()
    {
        Finish(); foreach (var c in State.catalogue.clues) State.Grant(c.id); foreach (var s in State.catalogue.statements) State.Grant(s.id);
        var ids = new[] { State.Construct("C09", "F-S04").id, State.Construct("C01", "F-S01").id };
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        foreach (var id in ids) Call(notebook, "TetherTheoryToAccusation", id);
        Call(notebook, "TetherAccusationToSuspect", "Mila"); yield return null; yield return null;
        var assessment = (AccusationAssessment)notebook.GetType().GetProperty("AccusationSupport").GetValue(notebook);
        Assert.That(assessment.Supported, Is.False); Assert.That(assessment.DisplayText, Does.Contain("[Missing] Motive"));
        Assert.That(notebook.GetComponentsInChildren<MonoBehaviour>().Where(c => c.GetType().Name == "Text").Any(t => ((string)t.GetType().GetProperty("text").GetValue(t)).Contains("[Missing] Motive")), Is.True);
        Call(notebook, "TetherTheoryToAccusation", ids[1]);
        var replacement = State.Construct("C06", "M-S06"); Call(notebook, "TetherTheoryToAccusation", replacement.id); Call(notebook, "TetherAccusationToSuspect", "Mila");
        yield return null; yield return null;
        assessment = (AccusationAssessment)notebook.GetType().GetProperty("AccusationSupport").GetValue(notebook);
        Assert.That(assessment.Supported, Is.True); Assert.That(assessment.DisplayText, Does.Contain("CASE SUPPORT: SUPPORTED"));
        Assert.That(State.progress.actionsSpent, Is.Zero);
    }
    [UnityTest] public IEnumerator ExplicitMMOSocketsAcceptTheReportedCaseWithoutTheOldContradictionPrerequisites()
    {
        Finish(); foreach (var id in new[] { "C03", "B-S01", "C06", "M-S06", "C09", "F-S04" }) State.Grant(id);
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        var pairs = new[] { new[] { "C06", "M-S06", AccusationReasoning.Motive }, new[] { "C09", "F-S04", AccusationReasoning.Opportunity } };
        foreach (var pair in pairs)
        {
            var theory = State.Construct(pair[0], pair[1]); yield return null; yield return null;
            Call(notebook, "SelectNode", theory.id); Call(notebook, "SelectNode", AccusationReasoning.SocketId(pair[2])); yield return null;
        }
        Call(notebook, "SelectNode", AccusationGraphLayout.DraftId); Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); yield return null;
        var graph = (ReasoningGraphLayout)notebook.GetType().GetProperty("Graph").GetValue(notebook);
        Assert.That(graph.nodes.Count(n => n.kind == ReasoningNodeKind.AccusationSocket), Is.EqualTo(2));
        Assert.That(State.Has("M-S05"), Is.False); Assert.That(State.Has("C10"), Is.False);
        Assert.That(((AccusationAssessment)notebook.GetType().GetProperty("AccusationSupport").GetValue(notebook)).Supported, Is.True);
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.True); Finish(); Assert.That(State.Has("flag:resolved"), Is.True);
    }
    [UnityTest] public IEnumerator SocketMovesPreventDuplicatesAndWrongRoleAssignmentsRemainUnsupported()
    {
        Finish(); foreach (var id in new[] { "C03", "B-S01", "C06", "M-S06", "C09", "F-S04" }) State.Grant(id);
        var ids = new[] { State.Construct("C03", "B-S01").id, State.Construct("C06", "M-S06").id, State.Construct("C09", "F-S04").id };
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Call(notebook, "TetherTheoryToSocket", ids[0], AccusationReasoning.Motive);
        Call(notebook, "TetherTheoryToSocket", ids[1], AccusationReasoning.Opportunity);
        Call(notebook, "TetherTheoryToSocket", ids[2], AccusationReasoning.Opportunity);
        Call(notebook, "TetherAccusationToSuspect", "Mila");
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.False);
        Call(notebook, "TetherTheoryToSocket", ids[0], AccusationReasoning.Opportunity);
        var sockets = (System.Collections.Generic.IReadOnlyDictionary<string, string>)notebook.GetType().GetProperty("AccusationSockets").GetValue(notebook);
        Assert.That(sockets.Count, Is.EqualTo(1)); Assert.That(sockets.Values.Distinct().Count(), Is.EqualTo(1));
        Call(notebook, "TetherTheoryToSocket", ids[1], AccusationReasoning.Motive); Call(notebook, "TetherTheoryToSocket", ids[2], AccusationReasoning.Opportunity); Call(notebook, "TetherAccusationToSuspect", "Mila");
        Assert.That(((AccusationAssessment)notebook.GetType().GetProperty("AccusationSupport").GetValue(notebook)).Supported, Is.True); yield return null;
    }
    [UnityTest] public IEnumerator CollectedClueAndStatementFillTwoInterpretationsAndResolve()
    {
        Finish(); State.Grant("C06"); State.Grant("F-S04");
        while (State.InvestigationOpen) State.EndDay(out _);
        var notebook = controller.GetComponent("InvestigationNotebook"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
        Call(notebook, "SelectNode", "C06"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Motive));
        Call(notebook, "SelectNode", "F-S04"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Opportunity));
        Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); yield return null;
        Assert.That(State.progress.theories, Is.Empty);
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.True);
        Finish(); Assert.That(State.progress.phase, Is.EqualTo(InvestigationPhase.Resolved));
    }
    [UnityTest] public IEnumerator EarlyAccusationIsAccessibleWithoutClosingTheDayAndFailureKeepsInvestigationOpen()
    {
        Finish(); State.Grant("C06"); State.Grant("F-S04");
        Call(controller, "ToggleNotebook"); yield return null; yield return null;
        var notebook = controller.GetComponent("InvestigationNotebook");
        var accuseButton = notebook.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b => b.GetComponentInChildren<UnityEngine.UI.Text>()?.text == "ACCUSE");
        Assert.That(accuseButton.interactable, Is.True); accuseButton.onClick.Invoke(); yield return null; yield return null;
        Assert.That((bool)notebook.GetType().GetProperty("AccusationGraphActive").GetValue(notebook), Is.True);
        Call(notebook, "SelectNode", "C06"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Motive));
        Call(notebook, "SelectNode", "F-S04"); Call(notebook, "SelectNode", AccusationReasoning.SocketId(AccusationReasoning.Opportunity));
        Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Felix"));
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.False);
        Assert.That(State.InvestigationOpen, Is.True); Assert.That(State.progress.day, Is.EqualTo(1));
        Assert.That(State.Grant("C01"), Is.True); Assert.That(State.Alibi("Mila"), Is.Not.Null);
        Call(notebook, "SelectNode", AccusationGraphLayout.DraftId); Call(notebook, "SelectNode", AccusationGraphLayout.SuspectId("Mila")); yield return null;
        yield return CaptureInteractionReview("early-accusation");
        Assert.That((bool)Call(notebook, "SubmitAccusation"), Is.True); Finish();
        Assert.That(State.progress.day, Is.EqualTo(1)); Assert.That(State.Has("flag:resolved"), Is.True);
    }
    [UnityTest] public IEnumerator InterpretationBadgesIdentifyCluesStatementsQuestionsAndBothRoles()
    {
        Finish(); foreach (var id in new[] { "C01", "C06", "C09", "M-S06", "F-S04" }) State.Grant(id);
        var motiveQuestion = State.Construct("C06", "M-S06"); var opportunityQuestion = State.Construct("C09", "F-S04");
        bool added = !motiveQuestion.propositions.Contains(AccusationReasoning.Opportunity);
        if (added) motiveQuestion.propositions.Add(AccusationReasoning.Opportunity);
        try
        {
            State.HintClue("C05", "Felix", "FELI_002"); Call(controller, "ToggleNotebook"); yield return null; yield return null;
            var notebook = controller.GetComponent("InvestigationNotebook");
            Transform Node(string id) => notebook.GetComponentsInChildren<MonoBehaviour>().Single(c => c.GetType().Name == "NotebookNodeGraphic" && c.name == id).transform;
            bool Badge(string id, string role) => Node(id).GetComponentsInChildren<RectTransform>().Any(r => r.name == "Interpretation badge / " + role);
            Assert.That(Badge("C06", "Motive"), Is.True); Assert.That(Badge("M-S06", "Motive"), Is.True);
            Assert.That(Badge("F-S04", "Opportunity"), Is.True); Assert.That(Badge(opportunityQuestion.id, "Opportunity"), Is.True);
            Assert.That(Badge(motiveQuestion.id, "Motive") && Badge(motiveQuestion.id, "Opportunity"), Is.True);
            Assert.That(Badge("C01", "Motive") || Badge("C01", "Opportunity"), Is.False);
            Assert.That(Badge("C05", "Motive") || Badge("C05", "Opportunity"), Is.False);
            yield return CaptureInteractionReview("interpretation-badges");
        }
        finally { if (added) motiveQuestion.propositions.Remove(AccusationReasoning.Opportunity); }
    }
    [UnityTest] public IEnumerator LegacyDialogueClueRewardBecomesHintInsteadOfRemoteEvidence()
    {
        Finish(); var beat = State.catalogue.Sequence("Felix — Champagne alibi").beats[3];
        bool alreadyPresent = beat.grants.Contains("C05"); if (!alreadyPresent) beat.grants.Add("C05");
        try
        {
            Approach("Felix"); Call(controller, "Begin", "Felix — Champagne alibi", null);
            for (int i = 0; i < 4; i++) Call(controller, "Advance");
            Assert.That(State.Has("C05"), Is.False); Assert.That(State.HasClueHint("C05"), Is.True);
            Assert.That((string)controller.GetType().GetProperty("LogbookNotice").GetValue(controller), Does.Contain("Clue hint: Champagne receipt"));
        }
        finally { if (!alreadyPresent) beat.grants.Remove("C05"); }
        yield return null;
    }
}
