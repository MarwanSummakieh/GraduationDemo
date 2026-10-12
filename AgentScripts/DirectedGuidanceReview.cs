using System;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Narrative;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.UI;

public static class DirectedGuidanceReview
{
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task<string> Run()
    {
        await Task.Delay(500);
        var n = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
        for (int i = 0; i < 50 && n.CurrentSequence != null; i++) n.Advance();
        foreach (var c in n.catalogue.clues) n.State.Grant(c.id);
        foreach (var s in n.catalogue.statements.Where(s => s.initial)) n.State.Grant(s.id);
        if (!n.NotebookOpen) n.ToggleNotebook();
        var book = n.GetComponent<InvestigationNotebook>(); book.SetAssistance(ReasoningAssistance.Directed);
        n.State.Construct("C02", "M-S03"); await Task.Delay(200); book.SelectNode("T_C02_MS03"); await Task.Delay(150);
        string detail = Detail(book);
        Check(detail.StartsWith("NEXT INTERVIEW: Bring this theory to Mila."), "Recipient advice was not at the top.");
        Check(detail.Contains("bring it to Mila"), "Directed recipient absent.");
        Check(!detail.Contains("B-S02"), "Selected theory inherited a different pairing's lead.");
        var hover = book.GetComponentsInChildren<NotebookHover>().Single(h => h.theoryId == "T_C02_MS03");
        Check(hover.text.Contains("bring it to Mila"), "Theory tooltip did not inherit its exact pairing guidance.");
        book.SetAssistance(ReasoningAssistance.Guided); await Task.Delay(150);
        Check(!Detail(book).Contains("bring it to Mila"), "Guided leaked recipient.");
        book.SetAssistance(ReasoningAssistance.Directed); n.State.Grant("M-S04"); await Task.Delay(150);
        Check(book.VisibleHints.Any(h => h.id == "desk_and_clumsiness"), "New statement did not open the next interview link.");
        n.State.Grant("M-S05"); await Task.Delay(150);
        Check(book.VisibleHints.Any(h => h.id == "clock_and_spatial_account"), "New spatial statement did not open case links.");
        Check(!book.VisibleHints.Any(h => h.id == "desk_and_clumsiness"), "Completed interview lead remained active.");
        n.State.Grant("F-S04"); n.State.Construct("C09", "F-S04"); await Task.Delay(150);
        book.SelectNode("T_C09_FS04"); await Task.Delay(150); detail = Detail(book);
        Check(detail.StartsWith("CASE REVIEW: No further interview"), "Case-only advice was not at the top.");
        Check(detail.Contains("no further interview") && !detail.Contains("bring it to"), "Reported theory still misdirected toward an interview.");
        Check(!detail.Contains("F-S01"), "Reported theory inherited the earlier bar-alibi lead.");
        foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" })
            Check(ReasoningGuidance.InterviewNote(n.catalogue, n.State.progress, n.State.Presentation("T_C09_FS04", suspect)).StartsWith("No new testimony"), "Reaction-only annotation absent.");
        for (int i = 0; i < 4; i++)
        {
            book.GetComponentsInChildren<Button>().Single(b => b.gameObject.name.StartsWith("Guidance:")).onClick.Invoke();
            await Task.Delay(100);
        }
        Check(book.Assistance == ReasoningAssistance.Directed, "Toolbar did not cycle all four layers.");
        return "PASS: Directed recipients on exact selected pairings and theory tooltips; Guided recipient suppression; new-statement interview and case links; reported C09/F-S04 case-only annotation; reaction-only notices for all suspects; four-level toolbar cycle.";
    }
    static string Detail(InvestigationNotebook book) => book.GetComponentsInChildren<Text>().First(t => t.text.Contains("REASONING NOTES")).text;
    public static async Task<string> PrepareCapture(string view)
    {
        var n = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
        var book = n.GetComponent<InvestigationNotebook>();
        n.CancelConfirmation(); if (!n.NotebookOpen) n.ToggleNotebook();
        book.SetAssistance(ReasoningAssistance.Directed); await Task.Delay(150);
        var theory = view == "interview" ? n.State.Construct("C06", "F-S02") ?? n.catalogue.Pair("C06", "F-S02") : n.catalogue.Pair("C09", "F-S04");
        book.GetComponentInChildren<InputField>().onEndEdit.Invoke(theory.text); await Task.Delay(150);
        book.SelectNode(theory.id); await Task.Delay(150);
        return Detail(book);
    }
    public static string ReactionConfirmation()
    {
        var n = UnityEngine.Object.FindAnyObjectByType<TrainNarrative>();
        Check(n.OfferConversation(n.State.Presentation("T_C09_FS04", "Mila")), "Could not show confirmation.");
        Check(n.State.progress.actionsSpent == 0, "Offer spent an action.");
        return "Reaction-only confirmation displayed before spending an action.";
    }
}
