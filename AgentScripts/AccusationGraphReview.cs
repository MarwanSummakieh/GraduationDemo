using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class AccusationGraphReview
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static InvestigationNotebook B => N.GetComponent<InvestigationNotebook>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Finish() { for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance(); }
    static async Task Click(string id, bool right = false)
    {
        var node = B.GetComponentsInChildren<NotebookNodeGraphic>().Single(n => n.gameObject.name == id);
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, node.transform.position);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(100);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }.WithButton(right ? MouseButton.Right : MouseButton.Left, true)); await Task.Delay(100);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(160);
    }
    static int Vertices()
    {
        var graphic = B.GetComponentsInChildren<NotebookStrokeGraphic>().Single(g => !g.map);
        using (var vertices = new VertexHelper())
        {
            typeof(NotebookStrokeGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Invoke(graphic, new object[] { vertices });
            return vertices.currentVertCount;
        }
    }
    public static async Task<string> Run()
    {
        await Task.Delay(500); Finish();
        foreach (var c in N.catalogue.clues) N.State.Grant(c.id);
        foreach (var s in N.catalogue.statements) N.State.Grant(s.id);
        int discoveries = N.State.progress.discoveries.Count;
        while (N.State.InvestigationOpen) N.State.EndDay(out _);
        if (!N.NotebookOpen) N.ToggleNotebook(); await Task.Delay(200);
        Check(B.AccusationGraphActive && N.State.progress.theories.Count == 0, "Final stage did not open an empty accusation graph.");
        var previous = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var faded = B.GetComponentsInChildren<NotebookNodeGraphic>().Single(n => n.gameObject.name == "C03");
            Check(faded.color.a < .4f, "Clue not de-emphasized.");
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = RectTransformUtility.WorldToScreenPoint(null, faded.transform.position) }); await Task.Delay(150);
            Check(B.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "Hover detail").gameObject.activeSelf, "Faded clue did not provide hover detail.");
            foreach (var pair in new[] { new[] { "C03", "B-S01" }, new[] { "C09", "F-S04" }, new[] { "C10", "M-S05" } })
            {
                await Click(pair[0]); await Click(pair[1]);
                Check(N.State.progress.theories.Contains(N.catalogue.Pair(pair[0], pair[1]).id), "Pointer did not construct final-stage theory.");
                await Click(AccusationGraphLayout.DraftId);
            }
            Check(B.AccusationTheories.Count == 3 && !B.CanSubmitAccusation, "Suspect tether was not required.");
            int beforeSuspect = Vertices(); await Click(AccusationGraphLayout.SuspectId("Mila"));
            Check(B.CanSubmitAccusation && Vertices() == beforeSuspect + 4, "Suspect tether was not drawn.");
            N.State.Construct("C01", "F-S01"); await Task.Delay(150);
            Check(!B.TetherTheoryToAccusation("T_C01_FS01"), "Fourth theory was accepted.");
            await Click("T_C03_BS01", true);
            Check(B.AccusationTheories.Count == 2 && B.AccusationSuspect == null && !B.CanSubmitAccusation, "Removal retained invalid case tethers.");
            Check(N.State.RestoreTheory("T_C03_BS01"), "Restore failed."); await Task.Delay(150);
            await Click("T_C03_BS01"); await Click(AccusationGraphLayout.DraftId); await Click(AccusationGraphLayout.SuspectId("Mila"));
            Check(B.CanSubmitAccusation, "Restored theory could not rejoin the accusation.");
            Check(N.State.progress.discoveries.Count == discoveries && N.State.progress.actionsSpent == 0, "Final reasoning changed discoveries or budget.");
            B.SelectNode(AccusationGraphLayout.DraftId); await Task.Delay(100);
            return "PASS: real mouse hover on faded clue; three new theories constructed after final closure; theory-to-accusation and accusation-to-suspect pointer tethers; visible suspect edge; fourth-theory guard; right-click removal/pruning/restoration; no discovery or action changes. Supported draft ready for review.";
        }
        finally { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); InputSystem.settings.editorInputBehaviorInPlayMode = previous; }
    }
    public static async Task<string> Submit()
    {
        Check(B.SubmitAccusation(), "Supported graph accusation rejected."); Finish(); await Task.Delay(100);
        Check(N.State.progress.phase == InvestigationPhase.Resolved, "Finale did not resolve the case.");
        return "PASS: graph submission played the supported Mila finale and resolved the case.";
    }
}
