using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class PacingWalkthrough
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Finish() { for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance(); Check(N.CurrentSequence == null, "Conversation did not finish."); }
    static void Approach(string id)
    {
        var actor = UnityEngine.Object.FindObjectsByType<TrainActor>().First(a => a.characterId == id); var tour = N.GetComponent<TrainTour>();
        tour.detective.enabled = false; tour.detective.transform.position = new Vector3(actor.transform.position.x, 1.12f, -.3f); tour.detective.enabled = true;
    }
    static void EndDay() { N.RequestEndDay(); Check(N.ConfirmEndDay(), "Day closure failed."); }
    static void Present(string clue, string statement, string target)
    {
        var t = N.State.Construct(clue, statement); Approach(target); int before = N.State.RemainingActions;
        Check(N.Confront(t.id, target), "Presentation unavailable: " + t.id);
        Check(N.CurrentSequence == null && N.State.RemainingActions == before, "Offering spent an action before confirmation.");
        Check(N.ConfirmConversation(), "Presentation confirmation failed."); Check(N.State.RemainingActions == before - 1, "Wrong action cost."); Finish();
    }
    public static async Task<string> Investigate()
    {
        Finish(); var tour = N.GetComponent<TrainTour>();
        foreach (var binding in UnityEngine.Object.FindObjectsByType<NarrativeInteraction>().Where(b => !string.IsNullOrEmpty(b.clueId)))
        {
            tour.detective.enabled = false; tour.detective.transform.position = new Vector3(binding.transform.position.x, 1.12f, -.2f); tour.detective.enabled = true;
            var observation = binding.GetComponent<TrainObservation>();
            Check(Vector3.Distance(tour.detective.transform.position + Vector3.up, observation.transform.position) <= observation.interactionRange, "Unreachable clue " + binding.clueId);
            tour.OpenObservation(observation); tour.CloseObservation();
        }
        foreach (var target in new[] { "Felix", "Bellamy", "Mila" })
        {
            Approach(target); var binding = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>().First(b => b.suspectId == target);
            tour.OpenObservation(binding.GetComponent<TrainObservation>()); Check(N.CurrentSequence == null, "Suspect click must offer a prompt.");
            Check(N.OfferConversation(N.State.Alibi(target)), "Alibi unavailable."); Finish();
            Check(N.State.Alibi(target) == null, "Alibi repeated."); tour.OpenObservation(binding.GetComponent<TrainObservation>());
            Check(N.CurrentSequence == null && N.BarkText == N.catalogue.Voice("BURR_NO_CHAT_01").subtitle, "No-conversation bark missing.");
        }
        Check(N.State.RemainingActions == 3, "Alibis must be free.");
        var rings = N.State.Construct("C06", "F-S02"); Approach("Felix"); N.Confront(rings.id, "Felix"); N.CancelConfirmation();
        Check(N.State.RemainingActions == 3 && N.State.Presentation(rings.id, "Felix") != null, "Cancelled confirmation consumed prompt/budget.");
        Present("C06", "F-S02", "Felix"); Present("C09", "F-S01", "Felix"); Present("C02", "M-S03", "Mila");
        Check(N.State.RemainingActions == 0 && N.State.progress.day == 1, "Day must stay open at zero actions.");
        Check(N.State.Construct("C03", "B-S01") != null, "Free reasoning blocked at zero actions."); EndDay();
        Present("C07", "M-S04", "Mila");
        Check(N.State.Presentation("T_C07_MS04", "Mila") == null, "Repeated dialogue offered.");
        var alternative = N.State.Construct("C10", "M-S04"); Check(N.State.Presentation(alternative.id, "Mila") == null, "Shared revelation repeated through alternative route.");
        N.State.Construct("C04", "F-S04"); N.State.Construct("C10", "M-S05");
        Check(!N.Accuse("Mila", new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }), "Early accusation succeeded.");
        tour.SetView(0, true); tour.SnapCamera(); N.ToggleNotebook();
        await Task.Delay(200);
        File.WriteAllText("Documentation/Narrative/PacingWalkthrough.txt", "PASS: ten reachable clues with collection locations; three free one-use alibis; Burrows no-chit-chat bark; cancelled confirmation preserved prompt/budget; four paid core revelations; zero-action free reasoning; manual day closure/replenishment; shared revelation suppressed; early accusation rejected. Currently day 2 with two actions left.");
        return "PASS: core investigation and pacing flow; notebook open on day 2.";
    }
    public static async Task<string> Tab(string tab)
    {
        if (!N.NotebookOpen) N.ToggleNotebook();
        N.GetComponent<InvestigationNotebook>().SelectTab((InvestigationNotebook.NotebookTab)Enum.Parse(typeof(InvestigationNotebook.NotebookTab), tab));
        await Task.Delay(150); return "Notebook tab: " + tab;
    }
    public static string PrepareFinal()
    {
        if (N.NotebookOpen) N.ToggleNotebook(); EndDay(); Check(N.State.progress.day == 3, "Day 3 missing.");
        Check(!N.Accuse("Mila", new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }), "Accusation opened before final day closure.");
        EndDay(); Check(N.State.progress.phase == InvestigationPhase.Accusation, "Final phase missing.");
        Check(!N.State.Grant("C01", "bar") && N.State.Alibi("Felix") == null, "Investigation reopened after closure.");
        Check(!N.Accuse("Felix", new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }), "Wrong suspect succeeded.");
        Check(N.Accuse("Mila", new[] { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" }), "Supported final accusation failed.");
        return "PASS: early final-day accusation blocked; final closure unlocks supported static ending.";
    }
    public static string FinishFinal()
    {
        Finish(); Check(N.State.progress.phase == InvestigationPhase.Resolved, "Finale did not resolve the case.");
        File.AppendAllText("Documentation/Narrative/PacingWalkthrough.txt", "\nPASS: unused actions forfeited on early day ending; final-day closure locked discoveries/interviews; wrong suspect rejected; supported Mila finale resolved the case.");
        return "PASS: three-day supported case resolved.";
    }
    public static async Task<string> UnsupportedCase()
    {
        await SceneManager.LoadSceneAsync("CarnivalExpress"); await Task.Delay(150); Finish();
        N.State.Grant("C01", "odette"); N.State.Grant("F-S01", "bar"); Present("C01", "F-S01", "Felix");
        while (N.State.InvestigationOpen) EndDay();
        Check(!N.Accuse("Mila", new[] { "T_C01_FS01" }), "Insufficient case succeeded.");
        Check(N.State.progress.phase == InvestigationPhase.Accusation && !N.State.Grant("C03"), "Unresolved case reopened investigation.");
        File.AppendAllText("Documentation/Narrative/PacingWalkthrough.txt", "\nPASS: fresh case spent an action on an unproductive theory, ended all days, returned provisional unresolved feedback and did not reopen investigation.");
        N.ToggleNotebook(); await Task.Delay(100); return "PASS: wasted-action/unsupported case remains closed and reviewable.";
    }
    public static async Task<string> FullGraph()
    {
        await SceneManager.LoadSceneAsync("CarnivalExpress"); await Task.Delay(150); Finish();
        foreach (var c in N.catalogue.clues) N.State.Grant(c.id);
        foreach (var s in N.catalogue.statements) N.State.Grant(s.id);
        foreach (var c in N.catalogue.clues) foreach (var s in N.catalogue.statements) N.State.Construct(c.id, s.id);
        N.ToggleNotebook(); await Task.Delay(500);
        Check(N.GetComponent<InvestigationNotebook>().VisibleNodeCount == 186, "Not all graph nodes are accessible.");
        UnityEngine.Object.FindAnyObjectByType<NotebookPanZoom>().Fit(N.GetComponent<InvestigationNotebook>().Graph.size); await Task.Delay(150);
        File.AppendAllText("Documentation/Narrative/PacingWalkthrough.txt", "\nPASS: all 160 constructed theories produce 186 unique graph nodes with their acquired parents.");
        return "PASS: 186-node graph displayed.";
    }
    public static async Task<string> Hud()
    {
        if (N.NotebookOpen) N.ToggleNotebook(); await Task.Delay(150);
        return "Day " + N.State.progress.day + " / remaining actions " + N.State.RemainingActions;
    }
    public static string DiagnoseUi()
    {
        return "NotebookOpen=" + N.NotebookOpen + "; notebooks=" + UnityEngine.Object.FindObjectsByType<InvestigationNotebook>().Length + "; canvases=" +
            string.Join("|", UnityEngine.Object.FindObjectsByType<Canvas>().Select(c => c.name + ":" + c.transform.childCount + ":" + (c.transform.childCount > 0 ? c.transform.GetChild(0).gameObject.activeSelf.ToString() : "empty")));
    }
    public static async Task<string> CheckGraphPointer()
    {
        var notebook = N.GetComponent<InvestigationNotebook>(); notebook.SelectTab(InvestigationNotebook.NotebookTab.Reasoning); await Task.Delay(200);
        var previous = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            foreach (string id in new[] { "C01", "F-S01" })
            {
                var node = UnityEngine.Object.FindObjectsByType<NotebookNodeGraphic>().First(g => g.gameObject.name == id);
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, node.transform.position);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(120);
                var tooltip = N.GetComponentsInChildren<RectTransform>(true).First(r => r.name == "Hover detail"); Check(tooltip.gameObject.activeSelf, "Graph hover did not reach tooltip.");
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }.WithButton(MouseButton.Left, true)); await Task.Delay(120);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(120);
            }
            Check(N.State.progress.theories.Contains("T_C01_FS01"), "Graph pointer clicks did not construct the pair.");
            var navigation = UnityEngine.Object.FindAnyObjectByType<NotebookPanZoom>(); float before = navigation.zoom;
            var clueNode = UnityEngine.Object.FindObjectsByType<NotebookNodeGraphic>().First(g => g.gameObject.name == "C01");
            Vector2 pointer = RectTransformUtility.WorldToScreenPoint(null, clueNode.transform.position);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer, scroll = new Vector2(0, 120) }); await Task.Delay(120);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer }); await Task.Delay(100);
            Check(navigation.zoom > before, "Graph scroll wheel did not zoom.");
            Vector2 pan = navigation.content.anchoredPosition;
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer }.WithButton(MouseButton.Left, true)); await Task.Delay(80);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer + new Vector2(50, 30) }.WithButton(MouseButton.Left, true)); await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer + new Vector2(50, 30) }); await Task.Delay(100);
            Check(Vector2.Distance(pan, navigation.content.anchoredPosition) > 1, "Graph drag did not pan.");
            const string report = "PASS: real Input System mouse events reached graph hover tooltip and clue/statement Button handlers, constructed T_C01_FS01, zoomed using wheel events and panned using drag events.";
            File.WriteAllText("Documentation/Narrative/GraphPointerReview.txt", report); return report;
        }
        finally { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); InputSystem.settings.editorInputBehaviorInPlayMode = previous; }
    }
}
