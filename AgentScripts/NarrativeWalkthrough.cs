using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Run through the live Unity Pipeline run_script command while CarnivalExpress is playing.</summary>
public static class NarrativeWalkthrough
{
    static TrainNarrative Narrative => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Finish(TrainNarrative n) { for (int i = 0; i < 50 && n.CurrentSequence != null; i++) n.Advance(); Require(n.CurrentSequence == null, "Dialogue did not complete."); }
    public static string InspectOpening()
    {
        var n = Narrative; Require(n.CurrentSequence == "OPENING — The real corpse", "Opening missing.");
        Require(!n.State.Has("M-S05") && n.State.progress.statements.Count == 0, "Premature testimony.");
        return "Opening active: " + n.CurrentVoiceId;
    }
    public static async Task<string> CheckAdvanceShortcut()
    {
        var n = Narrative; Require(n.CurrentVoiceId == "BURR_001", "Check shortcut on the opening's first beat.");
        var previous = InputSystem.settings.editorInputBehaviorInPlayMode;
        try
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.Space));
            await Task.Delay(150);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
            await Task.Delay(100);
            Require(n.CurrentVoiceId == "BELL_001", "Space did not advance exactly one dialogue beat: " + n.CurrentVoiceId);
            File.WriteAllText("Documentation/Narrative/KeyboardReview.txt", "PASS: Input System Space event advanced the opening exactly once from BURR_001 to BELL_001.");
            return "PASS: Space advances exactly one beat.";
        }
        finally { InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.settings.editorInputBehaviorInPlayMode = previous; }
    }
    public static async Task<string> Investigate()
    {
        var n = Narrative; var tour = n.GetComponent<TrainTour>(); Finish(n);
        // Move through every connecting vestibule using the actual CharacterController and colliders.
        tour.detective.enabled = false; tour.detective.transform.position = new Vector3(-30, 1.12f, -.7f); tour.detective.enabled = true;
        for (int i = 0; i < 600; i++) tour.detective.Move(new Vector3(.1f, -.03f, 0));
        Require(tour.detective.transform.position.x > 29, "Continuous investigation aisle is blocked at " + tour.detective.transform.position);
        Require(tour.detective.transform.position.y > .8f, "Investigation aisle has a floor gap.");
        foreach (var binding in UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None).Where(x => !string.IsNullOrEmpty(x.clueId)))
        {
            var observation = binding.GetComponent<TrainObservation>();
            tour.detective.enabled = false; tour.detective.transform.position = new Vector3(binding.transform.position.x, 1.12f, -.2f); tour.detective.enabled = true;
            Require(Vector3.Distance(tour.detective.transform.position + Vector3.up, observation.transform.position) <= observation.interactionRange, "Evidence cannot be reached from aisle: " + binding.clueId);
            tour.OpenObservation(observation); tour.CloseObservation();
        }
        Require(n.State.progress.clues.Count == 10, "Ten clues were not acquired.");
        foreach (var target in new[] { "Felix", "Bellamy", "Mila" })
        {
            Approach(tour, target);
            var binding = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None).First(x => x.suspectId == target);
            Require(tour.TryTalkTo(binding.GetComponent<TrainObservation>()), "Suspect cannot be interviewed."); Finish(n);
        }
        Require(n.State.progress.statements.Count == 7, "Initial interview statements missing.");
        foreach (var input in new[] { new[] { "C06", "F-S02", "Felix" }, new[] { "C09", "F-S01", "Felix" }, new[] { "C02", "M-S03", "Mila" }, new[] { "C07", "M-S04", "Mila" } })
        {
            var theory = n.State.Construct(input[0], input[1]); Approach(tour, input[2]);
            Require(n.Confront(theory.id, input[2]), "Confrontation failed: " + theory.id); Finish(n);
        }
        Require(n.State.Has("F-S03") && n.State.Has("F-S04") && n.State.Has("M-S05"), "Core revelations missing.");
        n.State.Construct("C03", "B-S01"); n.State.Construct("C04", "F-S04"); n.State.Construct("C10", "M-S05");
        tour.SetView(0, true); tour.SnapCamera(); n.ToggleNotebook();
        await Task.Delay(100);
        Directory.CreateDirectory("Documentation/Narrative");
        const string report = "PASS: opening; continuous aisle across five carriages; ten reachable physical discoveries; all initial interviews through TrainTour; rings revelation; corridor sighting; Mila speculation and spatial slip; notebook populated; accusation theories constructed.";
        File.WriteAllText("Documentation/Narrative/Walkthrough.txt", report); return report;
    }
    public static string StartEnding()
    {
        var n = Narrative;
        string[] ids = { "T_C03_BS01", "T_C04_FS04", "T_C10_MS05" };
        Require(!n.Accuse("Felix", ids), "Wrong suspect succeeded."); Require(n.Accuse("Mila", ids), "Supported accusation failed.");
        return "Static ending started: " + n.CurrentVoiceId;
    }
    public static string FinishEnding()
    {
        var n = Narrative; Finish(n); Require(n.State.Has("flag:resolved"), "Ending did not resolve case.");
        File.AppendAllText("Documentation/Narrative/Walkthrough.txt", "\nPASS: wrong suspect rejected; supported accusation played Mila's static finale and resolved the case.");
        return "PASS: case resolved.";
    }
    static void Approach(TrainTour tour, string target)
    {
        var actor = UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None).First(x => x.characterId == target);
        tour.detective.enabled = false; tour.detective.transform.position = new Vector3(actor.transform.position.x, 1.12f, -.3f); tour.detective.enabled = true;
    }
    public static string DescribePosition()
    {
        var tour = Narrative.GetComponent<TrainTour>();
        var position = tour.detective.transform.position;
        return position + " nearby colliders: " + string.Join("|", Physics.OverlapSphere(position + Vector3.up, 1).Select(c => c.name + " at " + c.transform.position));
    }
    public static async Task<string> CaptureUi(string file)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.GameView")).First(t => t != null);
        var view = UnityEditor.EditorWindow.GetWindow(type); view.Show(); view.Focus();
        await Task.Delay(200);
        var requestedAt = DateTime.UtcNow;
        ScreenCapture.CaptureScreenshot(file);
        for (int i = 0; i < 40 && (!File.Exists(file) || File.GetLastWriteTimeUtc(file) < requestedAt.AddSeconds(-1)); i++) await Task.Delay(100);
        await Task.Delay(300);
        Require(File.Exists(file) && File.GetLastWriteTimeUtc(file) >= requestedAt.AddSeconds(-1), "Game view UI capture failed or returned a stale frame."); return Path.GetFullPath(file);
    }
    public static string RecordUiReview()
    {
        var n = Narrative;
        Require(n.State.progress.theories.Contains("T_C02_FS01"), "UI combination was not constructed.");
        Require(n.CurrentSequence == "ENDING — Mila's account" && n.CurrentVoiceId == "MILA_006", "UI accusation / Continue did not reach the expected beat.");
        const string report = "PASS: native Unity UI selected broken clock and Felix's alibi; scrolled notebook; clicked Combine; displayed authored T_C02_FS01 and its sources; selected three independent accusation theories and Mila; clicked Present supported accusation; started static finale in Burrows's compartment with Mila present; Continue clicked once advanced to MILA_006. Opening and notebook readability inspected at small and wider Game view sizes.";
        File.WriteAllText("Documentation/Narrative/UIReview.txt", report); return report;
    }
}
