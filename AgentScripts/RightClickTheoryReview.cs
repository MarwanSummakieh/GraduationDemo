using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GraduationDemo.Narrative;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class RightClickTheoryReview
{
    public static string Status()
    {
        var n = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
        var b = n.GetComponent<InvestigationNotebook>();
        return $"paused={EditorApplication.isPaused}, notebook={n.NotebookOpen}, tab={b.Tab}, active nodes={b.VisibleNodeCount}, theories={string.Join(",", n.State.progress.theories)}, hover={string.Join(",",b.GetComponentsInChildren<NotebookHover>(true).Select(h=>h.gameObject.name + ":" + h.theoryId))}";
    }
    public static string SaveConfiguration()
    {
        var configuration = AssetDatabase.LoadAssetAtPath<InvestigationConfiguration>("Assets/Narrative/Data/InvestigationPacing.asset");
        Check(configuration.reasoningAssistance == ReasoningAssistance.Subtle && configuration.reasoningHints.Count == 23, "Missing authored defaults.");
        EditorUtility.SetDirty(configuration); AssetDatabase.SaveAssets();
        return "Saved interview and case-review hints with Subtle default in InvestigationPacing.asset.";
    }
    public static string UpdateHintCoverage()
    {
        var configuration = AssetDatabase.LoadAssetAtPath<InvestigationConfiguration>("Assets/Narrative/Data/InvestigationPacing.asset");
        configuration.reasoningHints = ReasoningGuidance.DefaultHints();
        EditorUtility.SetDirty(configuration); AssetDatabase.SaveAssets();
        return "Updated hint coverage to ten interview leads and thirteen case-review connections; retained pacing and assistance settings.";
    }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<string> Run()
    {
        await Task.Delay(500);
        var n = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
        for (int i = 0; i < 50 && n.CurrentSequence != null; i++) n.Advance();
        n.State.Grant("C01"); n.State.Grant("F-S01");
        var theory = n.State.Construct("C01", "F-S01");
        if (!n.NotebookOpen) n.ToggleNotebook();
        await Task.Delay(250);
        var book = n.GetComponent<InvestigationNotebook>();
        var hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.theoryId == theory.id);
        Check(hover.text.Contains("Right-click to remove theory"), "Missing tooltip instruction.");
        Check(!book.GetComponentsInChildren<Text>().Any(t => t.text == "Remove theory"), "Remove button remains.");
        var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check(n.State.progress.theories.Contains(theory.id), "Left click removed theory.");
        pointer.button = PointerEventData.InputButton.Right;
        var caption = hover.GetComponentsInChildren<Text>().Single(t => t.raycastTarget);
        ExecuteEvents.ExecuteHierarchy(caption.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        await Task.Delay(150);
        Check(!n.State.progress.theories.Contains(theory.id) && !book.Graph.nodes.Any(x => x.id == theory.id), "Right click did not remove node.");
        Check(n.State.Has("C01") && n.State.Has("F-S01"), "Sources lost.");
        Check(n.State.RestoreTheory(theory.id), "Restoration failed.");
        await Task.Delay(150);
        hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.theoryId == theory.id);
        ExecuteEvents.Execute(hover.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        Check(!n.State.progress.theories.Contains(theory.id), "Icon right click failed.");
        Check(n.State.RestoreTheory(theory.id), "Second restoration failed.");
        await Task.Delay(150);
        var inputBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.theoryId == theory.id);
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, hover.transform.position);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }.WithButton(MouseButton.Right, true)); await Task.Delay(100);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(150);
            Check(!n.State.progress.theories.Contains(theory.id), "Real Input System right click did not remove theory.");
            Check(n.State.RestoreTheory(theory.id), "Restoration after real pointer failed.");
        }
        finally { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); InputSystem.settings.editorInputBehaviorInPlayMode = inputBehavior; }
        n.State.Grant("C09"); n.State.Grant("M-S03");
        await Task.Delay(150);
        Check(book.Assistance == ReasoningAssistance.Subtle, "Wrong default guidance.");
        Check(book.VisibleHints.Any(h => h.id == "rehearsal_bar_context"), "Discovered ambiguity missing.");
        Check(!book.VisibleHints.Any(h => h.id == "clock_and_happy_mood"), "Undiscovered testimony leaked.");
        n.State.Grant("C02"); await Task.Delay(150);
        Check(book.VisibleHints.Any(h => h.id == "clock_and_happy_mood"), "Discovered contradiction missing.");
        hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.gameObject.name == "M-S03");
        Check(!hover.text.Contains("C02") && !hover.text.Contains("A happy mood"), "Subtle revealed the counterpart or explanation.");
        Check(book.GetComponentsInChildren<Text>().Any(t => t.text == "!" || t.text == "?"), "Subtle node marks absent.");
        int subtleVertices = GraphVertices(book);
        book.SetAssistance(ReasoningAssistance.Off); await Task.Delay(150);
        Check(book.VisibleHints.Count == 0, "Off did not hide hints.");
        Check(GraphVertices(book) == subtleVertices, "Subtle drew extra hint connections.");
        book.SetAssistance(ReasoningAssistance.Guided); await Task.Delay(150);
        Check(GraphVertices(book) > subtleVertices, "Guided dotted connections absent.");
        hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.gameObject.name == "M-S03");
        Check(hover.text.Contains("A happy mood"), "Guided explanation absent.");
        book.SelectNode("M-S03"); await Task.Delay(150);
        Check(book.GetComponentsInChildren<Text>().Any(t => t.text.Contains("REASONING NOTES") && t.text.Contains("A happy mood")), "Scrollable guidance detail absent.");
        return "PASS: icon/caption right-click handlers and real Input System right click; sources/restoration; Subtle node marks with no counterpart text or dotted geometry; Off suppression; Guided dotted geometry, tooltip and scrollable explanations.";
    }
    static int GraphVertices(InvestigationNotebook book)
    {
        var graphic = book.GetComponentsInChildren<NotebookStrokeGraphic>().Single(g => !g.map);
        using (var vertices = new VertexHelper())
        {
            typeof(NotebookStrokeGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Invoke(graphic, new object[] { vertices });
            return vertices.currentVertCount;
        }
    }
}
