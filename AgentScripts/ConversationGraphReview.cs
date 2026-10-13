using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public static class ConversationGraphReview
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static InvestigationNotebook B => N.GetComponent<InvestigationNotebook>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Pointer(RectTransform rect, bool click)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(120);
        if (!click) return;
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }.WithButton(MouseButton.Left, true)); await Task.Delay(100);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = screen }); await Task.Delay(200);
    }
    static RectTransform Node(string id) => (RectTransform)B.GetComponentsInChildren<NotebookNodeGraphic>().Single(g => g.name == id).transform;
    static int Vertices()
    {
        using (var vh = new VertexHelper())
        {
            var graphic = B.GetComponentsInChildren<NotebookStrokeGraphic>().Single(g => !g.map);
            typeof(NotebookStrokeGraphic).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly).Invoke(graphic, new object[] { vh });
            return vh.currentVertCount;
        }
    }
    public static async Task<string> Run()
    {
        await Task.Delay(500); for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance();
        N.State.Grant("C06"); N.State.Grant("F-S02"); N.ToggleNotebook(); B.SetAssistance(ReasoningAssistance.Directed); await Task.Delay(500);
        int before = Vertices(); Check(before > 8, "Unbuilt guidance line absent.");
        var behavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            await Pointer(Node("C06"), true); await Pointer(Node("F-S02"), true);
            Check(Vertices() == 8, "Constructed theory retained dotted guidance.");
            B.RemoveTheory("T_C06_FS02"); await Task.Delay(200); Check(Vertices() == before, "Removal did not restore dotted guidance.");
            N.State.RestoreTheory("T_C06_FS02"); await Task.Delay(200); Check(Vertices() == 8, "Restored theory retained dotted guidance.");
            N.ToggleNotebook(); N.State.Grant("C01"); N.State.Grant("F-S01"); N.State.Construct("C01", "F-S01");
            var actor = UnityEngine.Object.FindObjectsByType<TrainActor>().Single(a => a.characterId == "Felix");
            var tour = N.GetComponent<TrainTour>(); tour.SetView(1, true); tour.detective.enabled = false;
            tour.detective.transform.position = new Vector3(actor.transform.position.x, 1.12f, -.3f); tour.detective.enabled = true; tour.SnapCamera();
            var binding = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>().Single(x => x.suspectId == "Felix");
            N.Interact(binding.GetComponent<TrainObservation>()); await Task.Delay(500);
            Check(B.ConversationGraphActive && B.ConversationSuspect == "Felix", "Suspect did not open conversation graph.");
            var source = Node("C06").GetComponent<NotebookNodeGraphic>(); Check(source.color.r == source.color.g && source.color.a < .4f, "Sources not greyed.");
            await Pointer(Node("C06"), false);
            Check(B.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "Hover detail").gameObject.activeSelf, "Greyed source hover missing.");
            await Pointer(Node("T_C06_FS02"), true);
            var present = B.GetComponentsInChildren<Button>().Single(b => b.name == "Present theory to Felix");
            Check(present.interactable, "Theory presentation not enabled."); await Pointer((RectTransform)present.transform, true);
            Check(!N.NotebookOpen && N.CurrentSequence == null && N.State.progress.actionsSpent == 0, "Presentation skipped confirmation or spent early.");
            N.CancelConfirmation(); Check(N.State.Presentation("T_C06_FS02", "Felix") != null, "Cancel consumed prompt.");
            N.Interact(binding.GetComponent<TrainObservation>()); await Task.Delay(500); B.SelectNode("T_C06_FS02"); await Task.Delay(150);
            return "PASS: real pointer construction hides dotted hint; removal restores it; restoration hides it again; suspect graph contains available theories with grey hoverable sources; pointer presentation requires confirmation; cancel preserves budget and prompt.";
        }
        finally { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); InputSystem.settings.editorInputBehaviorInPlayMode = behavior; }
    }
}
