using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class MMOSocketReview
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static InvestigationNotebook B => N.GetComponent<InvestigationNotebook>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static async Task Click(string id)
    {
        var rect = (RectTransform)B.GetComponentsInChildren<NotebookNodeGraphic>().Single(g => g.name == id).transform;
        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); await Task.Delay(100);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }.WithButton(MouseButton.Left, true)); await Task.Delay(100);
        InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = point }); await Task.Delay(180);
    }
    public static async Task<string> Run()
    {
        await Task.Delay(500); for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance();
        foreach (var id in new[] { "C03", "B-S01", "C06", "M-S06", "C09", "F-S04" }) N.State.Grant(id);
        while (N.State.InvestigationOpen) N.State.EndDay(out _);
        if (!N.NotebookOpen) N.ToggleNotebook(); await Task.Delay(200);
        var previous = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var inputs = new[] { new[] { "C03", "B-S01", AccusationReasoning.Means }, new[] { "C06", "M-S06", AccusationReasoning.Motive }, new[] { "C09", "F-S04", AccusationReasoning.Opportunity } };
            foreach (var input in inputs)
            {
                await Click(input[0]); await Click(input[1]);
                var socket = B.GetComponentsInChildren<NotebookNodeGraphic>().Single(g => g.name == AccusationReasoning.SocketId(input[2]));
                Check(Mathf.Abs(socket.color.g - .82f) < .01f, "Compatible socket did not highlight.");
                await Click(AccusationReasoning.SocketId(input[2]));
            }
            await Click(AccusationGraphLayout.DraftId); await Click(AccusationGraphLayout.SuspectId("Mila"));
            Check(B.AccusationSupport.Supported && B.AccusationSockets.Count == 3, "Reported combination not supported in correct sockets.");
            Check(!N.State.Has("M-S05") && !N.State.Has("C10"), "Fixture relied on removed contradiction prerequisites.");
            await Click("T_C06_MS06"); await Click(AccusationReasoning.SocketId(AccusationReasoning.Means));
            Check(B.AccusationSockets.Count == 2 && B.AccusationSockets.Values.Distinct().Count() == 2 && !B.AccusationSupport.Supported, "Moving a theory duplicated it or left a supported wrong-role draft.");
            await Click("T_C03_BS01"); await Click(AccusationReasoning.SocketId(AccusationReasoning.Means));
            await Click("T_C06_MS06"); await Click(AccusationReasoning.SocketId(AccusationReasoning.Motive));
            await Click(AccusationGraphLayout.DraftId); await Click(AccusationGraphLayout.SuspectId("Mila"));
            Check(B.AccusationSupport.Supported && N.State.progress.actionsSpent == 0, "Corrected slots not supported or reasoning spent an action.");
            return "PASS: real mouse construction, compatible-socket highlighting and explicit Means/Motive/Opportunity placement; exactly three distinct attachments; wrong-role moves rejected and corrected; reported hypothesis supported without spatial-slip prerequisites; no action spending.";
        }
        finally { InputSystem.QueueStateEvent(Mouse.current, new MouseState()); InputSystem.settings.editorInputBehaviorInPlayMode = previous; }
    }
    public static string Submit()
    {
        Check(B.SubmitAccusation(), "MMO hypothesis rejected.");
        Check(N.catalogue.Sequence(N.CurrentSequence).beats.Any(b => b.voiceId == "BURR_MMO_FINALE_01"), "Finale retained contradiction-only bridge.");
        for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance();
        Check(N.State.Has("flag:resolved"), "Finale did not resolve the case.");
        return "PASS: MMO submission used the new bridge and retained the accidental-death confession and resolved ending.";
    }
}
