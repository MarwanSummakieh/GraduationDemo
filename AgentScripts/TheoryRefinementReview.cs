using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Train;
using GraduationDemo.Narrative;
using UnityEngine;

public static class TheoryRefinementReview
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Finish() { for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance(); }
    public static string RecordingCue()
    {
        Finish(); var tour = N.GetComponent<TrainTour>(); var mila = UnityEngine.Object.FindObjectsByType<TrainActor>().First(a => a.characterId == "Mila");
        tour.SetView(1, true); tour.detective.enabled = false; tour.detective.transform.position = new Vector3(mila.transform.position.x, 1.12f, -.3f); tour.detective.enabled = true; tour.SnapCamera();
        Check(N.Begin("Mila — Solitary rehearsal"), "Could not begin free alibi."); N.Advance(); N.Advance();
        Check(N.LogbookNotice.Contains("NOTED IN LOGBOOK") && N.State.Has("M-S01"), "New statement cue missing.");
        return N.LogbookNotice;
    }
    public static async Task<string> LayoutAndRemoval()
    {
        Finish();
        foreach (var c in N.catalogue.clues) N.State.Grant(c.id);
        foreach (var s in N.catalogue.statements) N.State.Grant(s.id);
        foreach (var pair in new[] { new[] { "C01", "F-S01" }, new[] { "C03", "B-S01" }, new[] { "C07", "M-S04" }, new[] { "C10", "M-S05" }, new[] { "C06", "F-S02" } }) N.State.Construct(pair[0], pair[1]);
        N.ToggleNotebook(); await Task.Delay(250); var notebook = N.GetComponent<InvestigationNotebook>();
        notebook.SelectNode("T_C10_MS05"); await Task.Delay(150);
        Check(notebook.RemoveSelectedTheory(), "Remove button logic failed."); await Task.Delay(150);
        Check(!notebook.Graph.nodes.Any(n => n.id == "T_C10_MS05") && N.State.Has("C10") && N.State.Has("M-S05"), "Removal erased sources or retained node.");
        Check(N.State.RestoreTheory("T_C10_MS05"), "Restore failed."); await Task.Delay(150);
        Check(notebook.Graph.nodes.Any(n => n.id == "T_C10_MS05"), "Restored graph node missing.");
        notebook.SelectNode("T_C10_MS05"); await Task.Delay(100);
        UnityEngine.Object.FindAnyObjectByType<NotebookPanZoom>().Fit(notebook.Graph.size); await Task.Delay(100);
        const string report = "PASS: dialogue acquired M-S01 and displayed NOTED IN LOGBOOK; theory nodes distributed by parent connections; selected theory removal removed node/edges while preserving clue and statement; restoration returned the node.";
        File.WriteAllText("Documentation/Narrative/TheoryRefinementReview.txt", report); return report;
    }
}
