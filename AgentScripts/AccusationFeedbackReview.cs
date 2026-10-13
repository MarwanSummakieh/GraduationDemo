using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.UI;

public static class AccusationFeedbackReview
{
    static TrainNarrative N => UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
    static InvestigationNotebook B => N.GetComponent<InvestigationNotebook>();
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<string> ReportedCase()
    {
        await Task.Delay(500); for (int i = 0; i < 50 && N.CurrentSequence != null; i++) N.Advance();
        foreach (var c in N.catalogue.clues) N.State.Grant(c.id); foreach (var s in N.catalogue.statements) N.State.Grant(s.id);
        var ids = new[] { N.State.Construct("C03", "B-S01").id, N.State.Construct("C06", "M-S06").id, N.State.Construct("C09", "F-S04").id };
        while (N.State.InvestigationOpen) N.State.EndDay(out _);
        if (!N.NotebookOpen) N.ToggleNotebook(); await Task.Delay(200);
        foreach (var id in ids) B.TetherTheoryToAccusation(id);
        B.TetherAccusationToSuspect("Mila"); B.SelectNode(AccusationGraphLayout.DraftId); await Task.Delay(200);
        Check(!B.AccusationSupport.Supported, "Reported case unexpectedly supported.");
        Check(B.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("CASE SUPPORT: INCOMPLETE") && t.text.Contains("[Missing] Contradictory testimony")), "Live checklist not displayed at top.");
        return B.AccusationSupport.DisplayText;
    }
    public static async Task<string> ReplaceMotive()
    {
        B.TetherTheoryToAccusation("T_C06_MS06"); var theory = N.State.Construct("C10", "M-S05");
        B.TetherTheoryToAccusation(theory.id); B.TetherAccusationToSuspect("Mila"); B.SelectNode(AccusationGraphLayout.DraftId); await Task.Delay(200);
        Check(B.AccusationSupport.Supported, "Replacement did not update support.");
        Check(B.GetComponentsInChildren<Text>().Any(t => t.text.StartsWith("CASE SUPPORT: SUPPORTED")), "Supported state not visible.");
        return "PASS: replacing speculative motive with independently supported contradictory testimony updates the live checklist to supported; no actions spent.";
    }
}
