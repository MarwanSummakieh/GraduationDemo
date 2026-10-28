using System;
using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    // Presentation only. All topology, focus, combat and saved progress live in Narrative.
    public sealed class DoubtDefenseView : MonoBehaviour
    {
        public DoubtDefense Defense { get; private set; }
        public bool IsOpen => root != null && root.activeSelf;
        public bool ReviewingAccusation { get; private set; }
        public string SelectedEntry { get; private set; }
        TrainNarrative owner;
        GameObject root;
        RectTransform page;
        Font font;
        string inspection = "";
        FocusTowerKind? category;
        public DefenseFeedback Feedback { get; private set; }
        Text battleStatus;
        Action confirm;
        Dictionary<string, string> sockets;
        string suspect;
        Text previewText;
        public void SetPreview(string message) { if (previewText != null) previewText.text = message.Split('\n')[0]; }
        readonly Color ink = new Color(.045f, .025f, .095f), paper = new Color(.93f, .92f, 1);
        public bool OpenDoubt(string id)
        {
            owner = GetComponent<TrainNarrative>();
            var defense = owner.State.OpenDoubt(id); if (defense == null) return false;
            Defense = defense; SelectedEntry = null; category = null; inspection = "";
            Feedback = GetComponent<DefenseFeedback>() ?? gameObject.AddComponent<DefenseFeedback>();
            Feedback.Configure(owner.State.configuration); Feedback.Clear();
            showDetails = false; autoTime = 0;
            EnsureRoot(); DrawBoard(); return true;
        }
        void EnsureRoot()
        {
            if (root == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                root = new GameObject("Doubts and Focus canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                root.transform.SetParent(transform, false);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 140;
                var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1180, 820); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
                page = Panel(root.transform, "Focus page", ink, Vector2.zero, new Vector2(1180, 820));
                page.anchorMin = page.anchorMax = new Vector2(.5f, .5f); page.pivot = new Vector2(.5f, .5f); page.anchoredPosition = Vector2.zero;
            }
            root.SetActive(true);
        }
        void Clear()
        {
            foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        }
        RectTransform Panel(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color; var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(pos.x, -pos.y); rect.sizeDelta = size; return rect;
        }
        Text Label(Transform parent, string text, Vector2 pos, Vector2 size, int fontSize = 17)
        {
            var go = new GameObject("Text / " + text.Split('\n')[0], typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(pos.x, -pos.y); rect.sizeDelta = size;
            var label = go.GetComponent<Text>(); label.font = font; label.fontSize = fontSize; label.color = paper; label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; label.raycastTarget = false; return label;
        }
        RectTransform Button(string text, Action action, float x, float y, float width = 170, bool enabled = true)
        {
            var rect = Panel(page, text, new Color(.16f, .11f, .25f), new Vector2(x, y), new Vector2(width, 34));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.interactable = enabled;
            button.onClick.AddListener(() => action());
            var label = Label(rect, text, new Vector2(6, 2), new Vector2(width - 12, 30)); label.alignment = TextAnchor.MiddleCenter;
            return rect;
        }
        public bool DetailsVisible => showDetails;
        public bool Running => Defense != null && Defense.Draft.started && !Defense.Draft.finished;
        bool showDetails;
        float autoTime;
        public void StartDefense()
        {
            if (Defense == null || !Defense.Start()) { DrawBoard(); return; }
            autoTime = 0; showDetails = false; Feedback.Play(DefenseFeedback.Cue.Launch); DrawBoard();
        }
        void ResetDefense() { Defense.Reset(); Feedback.Clear(); autoTime = 0; showDetails = false; DrawBoard(); }
        void DrawBoard()
        {
            Defense.RefreshPressure();
            Clear();
            Label(page, "DEFEND YOUR QUESTION", new Vector2(22, 16), new Vector2(900, 34), 25);
            Label(page, "MIND PALACE / DOUBT DEFENSE", new Vector2(500, 24), new Vector2(460, 26), 14).color = NeuralInk.Cyan;
            Button(ReviewingAccusation ? "Return to review" : "Notebook", Close, 970, 16, 188);
            Label(page, Defense.Interpretation, new Vector2(22, 57), new Vector2(1120, 52), 20);
            battleStatus = Label(page, "Focus " + Defense.FocusUsed + " / " + owner.State.FocusCapacity + "   •   Doubt " + (Defense.Draft.finished ? owner.State.DoubtPressure : Defense.Draft.pressure) + "   •   " + Defense.Draft.waves + " wave(s)" + (Running ? "   •   Running" : Defense.Draft.finished ? "   •   Complete" : ""), new Vector2(22, 115), new Vector2(1120, 26), 20);
            Panel(page, "Focus capacity", new Color(.15f, .25f, .27f), new Vector2(22, 145), new Vector2(760, 4));
            Panel(page, "Focus deployed", new Color(.5f, .9f, .8f), new Vector2(22, 145), new Vector2(760 * Mathf.Clamp01((float)Defense.FocusUsed / Mathf.Max(1, owner.State.FocusCapacity)), 4));
            var boardRect = Panel(page, "Defense board", new Color(.035f, .025f, .085f), new Vector2(22, 154), new Vector2(760, 540));
            boardRect.gameObject.AddComponent<RectMask2D>();
            NeuralFieldGraphic.Attach(boardRect, Feedback.ReducedMotion);
            var surface = new GameObject("Board geometry", typeof(RectTransform), typeof(DoubtBoardGraphic)); surface.transform.SetParent(boardRect, false);
            var surfaceRect = surface.GetComponent<RectTransform>(); surfaceRect.anchorMin = Vector2.zero; surfaceRect.anchorMax = Vector2.one; surfaceRect.offsetMin = surfaceRect.offsetMax = Vector2.zero;
            var graphic = surface.GetComponent<DoubtBoardGraphic>(); graphic.view = this; graphic.raycastTarget = true;
            var centerLabel = Label(boardRect, "QUESTION", new Vector2(338, 305), new Vector2(84, 18), 12); centerLabel.alignment = TextAnchor.MiddleCenter;
            foreach (var tower in Defense.Draft.towers)
            {
                string letter = DoubtRules.Kind(owner.catalogue, tower.entryId) == FocusTowerKind.Projectile ? "C" : DoubtRules.Kind(owner.catalogue, tower.entryId) == FocusTowerKind.Slow ? "S" : "Q";
                Label(boardRect, letter, new Vector2((tower.x + .5f) * 760 / 17 - 6, (tower.y + .5f) * 540 / 13 - 10), new Vector2(18, 20), 15);
            }
            string status = Defense.Draft.finished ? (Defense.Draft.defended ? "DOUBT RESOLVED   /   Affected questions are ready for suspects." : "Defense failed. Questions remain insecure; rearrange and retry.") :
                "Shots • Slow • Buff  |  Red: enemy haste  |  Amber: tower weakness";
            var statusLabel = Label(page, status, new Vector2(22, 704), new Vector2(760, 28), 17);
            if (Defense.Draft.finished) statusLabel.color = Defense.Draft.defended ? new Color(.5f, 1, .75f) : new Color(1, .5f, .5f);
            previewText = Label(page, Defense.Draft.finished ? "New contradictions may raise doubt again. Defense is readiness to question, not proof." : Defense.Message ?? "This run handles accumulated doubt together. Place towers, then test defense.", new Vector2(22, 738), new Vector2(760, 30), 16);
            Button(Defense.Planning ? "Test defense" : Defense.Draft.finished ? "Back to reasoning" : "Defending…", () => { if (Defense.Planning) StartDefense(); else BackToReasoning(); }, 22, 774, 230, !Running);
            Button(Running ? "Stop and rearrange" : "Rearrange", ResetDefense, 264, 774, 210, !Defense.Planning && owner.State.DoubtPressure > 0);
            DrawPalette();
            if (showDetails) DrawDetails();
        }
        IEnumerable<string> AvailableMaterials(FocusTowerKind kind) => owner.State.progress.clues.Concat(owner.State.progress.statements).Concat(owner.State.progress.theories)
            .Where(id => DoubtRules.Kind(owner.catalogue, id) == kind && !Defense.Draft.towers.Any(t => t.entryId == id))
            .OrderByDescending(id => DoubtRules.Relevant(owner.catalogue, Defense.Definition, id)).ThenBy(id => id, StringComparer.Ordinal);
        public void SelectCategory(FocusTowerKind kind)
        {
            category = kind; SelectedEntry = AvailableMaterials(kind).FirstOrDefault(); inspection = ""; DrawBoard();
        }
        void DrawPalette()
        {
            Label(page, Defense.Planning ? "PLACE A TOWER" : "YOUR FORMATION", new Vector2(804, 154), new Vector2(354, 30), 23);
            var kinds = new[] { FocusTowerKind.Projectile, FocusTowerKind.Slow, FocusTowerKind.Buff };
            var names = new[] { "Shot", "Slow", "Buff" };
            var descriptions = new[] { "Clues discharge neural impulses.", "Statements slow nearby doubts.", "Questions amplify nearby synapses." };
            for (int i = 0; i < kinds.Length; i++)
            {
                var kind = kinds[i]; int available = AvailableMaterials(kind).Count(), cost = kind == FocusTowerKind.Buff ? 2 : 1;
                float y = 198 + i * 112;
                var card = Panel(page, "Tower category / " + names[i], category == kind ? new Color(.2f, .16f, .34f) : new Color(.085f, .055f, .16f), new Vector2(804, y - 8), new Vector2(354, 102));
                var categoryButton = Button((category == kind ? "| " : "") + names[i] + "  /  " + cost + " focus", () => SelectCategory(kind), 814, y, 334,
                    Defense.Planning && available > 0 && Defense.FocusUsed + cost <= owner.State.FocusCapacity);
                categoryButton.SetParent(card, false); categoryButton.anchoredPosition = new Vector2(10, -8);
                Label(card, descriptions[i] + "\n" + available + " available", new Vector2(12, 48), new Vector2(330, 48), 16);
            }
            string description = category.HasValue ? (SelectedEntry == null ? "No unused material in this category." : "Click anywhere clear to place.\nRelated evidence is selected first.") : "Choose Shot, Slow or Buff.\nClick a placed tower to move it.";
            if (SelectedEntry != null && !category.HasValue) description = "Selected: " + SelectedEntry + "\nClick a clear position to move it.";
            if (!Defense.Planning) description = Running ? "Defending your question…\nSelect a tower for details." : "Review your formation in Details.\nReturn to reasoning when ready.";
            Label(page, description, new Vector2(804, 538), new Vector2(354, 76), 18);
            Button("Details", () => { showDetails = true; DrawBoard(); }, 804, 634, 168);
            Button("Remove tower", RemoveSelected, 990, 634, 168, Defense.Planning && SelectedEntry != null && Defense.Draft.towers.Any(t => t.entryId == SelectedEntry));
            Button(Feedback.Muted ? "Sound: off" : "Sound: on", () => { Feedback.Muted = !Feedback.Muted; DrawBoard(); }, 804, 680, 168);
            Label(page, "Free placement | No grid\nWalls link automatically; a route stays open.\nFocus limits towers, not notebook questions.", new Vector2(804, 734), new Vector2(354, 72), 15);
        }
        void RemoveSelected()
        {
            var tower = Defense.Draft.towers.Find(t => t.entryId == SelectedEntry);
            if (tower != null) { Feedback.Emit(tower.Cell, new Color(.6f, .8f, .9f)); Defense.Remove(SelectedEntry); Feedback.Play(DefenseFeedback.Cue.Remove); }
            SelectedEntry = null; category = null; DrawBoard();
        }
        void Modal(string title)
        {
            Panel(page, "Details shade", new Color(.025f, .05f, .06f, .98f), Vector2.zero, new Vector2(1180, 820));
            Label(page, title, new Vector2(70, 56), new Vector2(1000, 50), 26);
            Button("Back to board", () => { showDetails = false; DrawBoard(); }, 920, 56, 190);
        }
        void DrawDetails()
        {
            Modal("About this question");
            string text = Defense.Interpretation + "\n\n" + Defense.Definition.explanation + "\nContradicting source: " + Defense.Definition.sourceId + " / " + owner.catalogue.EntryText(Defense.Definition.sourceId) +
                "\n\nThis run handles accumulated doubts together:\n" + string.Join("\n\n", Defense.Draft.pressureCaptured ? Defense.Definition.challenges : owner.State.PendingDoubts.Select(d => owner.catalogue.Theory(d.questionId).text + " / " + d.reason)) + "\n\n" + inspection;
            if (SelectedEntry != null) text += "\n\n" + DoubtRules.TowerDescription(owner.catalogue, Defense.Definition, Defense.Tuning, SelectedEntry);
            text += "\n\nDoubt pressure: " + Defense.Draft.pressure + "; waves: " + Defense.Draft.waves + ". Red areas accelerate invaders ×" + Defense.Tuning.environmentalInvaderSpeed + "; amber areas weaken towers ×" + Defense.Tuning.environmentalTowerMultiplier + ". Grey obstructions block placement and movement.";
            var lead = DoubtRules.Suggestion(owner.State, Defense.Definition);
            if (lead != null) text += "\n\nRelated question to consider: " + lead.text + "\nSources: " + lead.clueId + " + " + lead.statementId;
            text += "\n\nRejected walls:\n" + string.Join("\n", Defense.Walls.Where(w => !w.Accepted).Select(w => w.rejection).Distinct());
            ScrollText(text, new Vector2(70, 138), new Vector2(1040, 520));
            Button("Back to reasoning", BackToReasoning, 70, 714, 280);
            if (lead != null) Button("Add related question", () => { owner.State.Construct(lead.clueId, lead.statementId); DrawBoard(); }, 380, 714, 280, !Running);
        }
        void ScrollText(string value, Vector2 pos, Vector2 size)
        {
            var viewport = Panel(page, "Inspectable reasoning", new Color(.075f, .145f, .16f), pos, size); viewport.gameObject.AddComponent<RectMask2D>();
            var text = Label(viewport, value, new Vector2(8, 6), new Vector2(size.x - 16, 1000));
            float height = Mathf.Max(size.y, text.preferredHeight + 16); text.rectTransform.sizeDelta = new Vector2(size.x - 16, height);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = text.rectTransform; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            text.raycastTarget = true;
        }
        public void SelectEntry(string id) { category = null; SelectedEntry = id; inspection = ""; DrawBoard(); }
        public bool PlaceSelected(float x, float y)
        {
            bool result = SelectedEntry != null && Defense.Place(SelectedEntry, new Vector2(x, y));
            Feedback.Play(result ? DefenseFeedback.Cue.Place : DefenseFeedback.Cue.Reject);
            if (result) { Feedback.Emit(new Vector2(x, y), new Color(.5f, 1, .85f)); if (category.HasValue) SelectedEntry = AvailableMaterials(category.Value).FirstOrDefault(); }
            DrawBoard(); return result;
        }
        public void ClickPosition(Vector2 cell)
        {
            var tower = Defense.Draft.towers.OrderBy(t => Vector2.Distance(t.Cell, cell)).FirstOrDefault(t => Vector2.Distance(t.Cell, cell) < .4f);
            var invader = Defense.Draft.invaders.FirstOrDefault(i => Vector2.Distance(i.Cell, cell) < .4f && i.health > 0 && !i.reachedCenter);
            if (tower != null) { SelectEntry(tower.entryId); return; }
            if (invader != null) { inspection = "INVADER: " + Defense.Definition.challenges[invader.challengeIndex] + "\nHealth " + invader.health.ToString("0.#"); showDetails = true; DrawBoard(); return; }
            if (Vector2.Distance(cell, DoubtDefense.Center) < .6f) { SelectedEntry = null; showDetails = true; DrawBoard(); return; }
            if (SelectedEntry != null && Defense.Planning) PlaceSelected(cell.x, cell.y);
        }
        public void BackToReasoning()
        {
            ReviewingAccusation = false; Close(); GetComponent<InvestigationNotebook>().ReturnToReasoning();
        }
        public void Close()
        {
            if (ReviewingAccusation && Defense != null) { Defense = null; DrawAccusationReview(); }
            else { if (root != null) root.SetActive(false); if (Feedback != null) Feedback.Clear(); ReviewingAccusation = false; Defense = null; }
        }
        public void OpenAccusationReview(IReadOnlyDictionary<string, string> assignments, string target, Action onConfirm)
        {
            owner = GetComponent<TrainNarrative>(); sockets = assignments.ToDictionary(k => k.Key, k => k.Value); suspect = target; confirm = onConfirm;
            ReviewingAccusation = true; Defense = null; EnsureRoot(); DrawAccusationReview();
        }
        void DrawAccusationReview()
        {
            Clear();
            Label(page, "WHAT WOULD CHANGE YOUR MIND?", new Vector2(30, 24), new Vector2(1080, 42), 28);
            Label(page, "Accusation: " + suspect + ". Completing defense challenges is optional. A cleared rehearsal does not establish guilt.", new Vector2(30, 82), new Vector2(1080, 62), 21);
            int index = 0;
            foreach (var role in AccusationReasoning.Roles)
            {
                float y = 160 + index++ * 215; sockets.TryGetValue(role, out var entry);
                var d = DoubtRules.Available(owner.State).Where(item => item.role == role).OrderByDescending(item => DoubtRules.Relevant(owner.catalogue, item, entry)).FirstOrDefault();
                var review = d == null ? null : owner.State.progress.doubtReviews.LastOrDefault(r => r.doubtId == d.id);
                Label(page, AccusationReasoning.Label(role) + " / " + entry, new Vector2(30, y), new Vector2(1080, 30), 23);
                Label(page, owner.catalogue.EntryText(entry) + "\n" + (d?.explanation ?? (role == AccusationReasoning.Motive ? "Does this establish a reason to act, or merely a relationship?" : "Does this establish a chance to act, or merely presence nearby?")) +
                    "\n" + (review == null ? "Unexamined / uncertainty acknowledged." : review.outcome + ": " + review.note), new Vector2(30, y + 40), new Vector2(1080, 112), 19);
                if (d != null) { string id = d.id; Button("Revisit relevant doubt", () => OpenDoubt(id), 30, y + 157, 280); }
            }
            Button("Revise evidence assignments", () => { ReviewingAccusation = false; Close(); }, 30, 680, 330);
            Button("Proceed with uncertainty", () => { var action = confirm; ReviewingAccusation = false; Close(); action?.Invoke(); }, 390, 680, 330);
            Label(page, "You may continue investigating after an unsupported early accusation. Review and doubt outcomes do not unlock or guarantee the ending.", new Vector2(30, 740), new Vector2(1080, 60), 19);
        }
        void Update()
        {
            if (IsOpen && !owner.NotebookOpen) { ReviewingAccusation = false; Close(); }
            if (!IsOpen || !Running) return;
            autoTime += Time.unscaledDeltaTime; int count = 0;
            while (autoTime >= Defense.Tuning.stepSeconds && count++ < 4 && Running)
            { autoTime -= Defense.Tuning.stepSeconds; Feedback.Step(Defense); }
            if (count == 0) return;
            if (Defense.Draft.finished) { showDetails = false; autoTime = 0; DrawBoard(); }
            else
            {
                if (battleStatus != null) battleStatus.text = "Focus " + Defense.FocusUsed + " / " + owner.State.FocusCapacity + "   |   Wave " + Mathf.Min(Defense.Draft.waves, 1 + Mathf.Max(0, Defense.Draft.spawned - 1) / Defense.Tuning.enemiesPerWave) + " / " + Defense.Draft.waves + "   |   Breaches " + Defense.Draft.breaches;
                if (previewText != null) previewText.text = Defense.Message;
                foreach (var graphic in root.GetComponentsInChildren<DoubtBoardGraphic>()) graphic.SetVerticesDirty();
            }
        }
        void OnDestroy() { if (root != null) Destroy(root); }
    }
}
