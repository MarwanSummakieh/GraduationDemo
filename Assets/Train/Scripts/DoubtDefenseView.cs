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
        int palettePage;
        Action confirm;
        Dictionary<string, string> sockets;
        string suspect;
        Text previewText;
        public void SetPreview(string message) { if (previewText != null) previewText.text = message.Split('\n')[0]; }
        readonly Color ink = new Color(.045f, .085f, .10f), paper = new Color(.94f, .88f, .72f);
        public bool OpenDoubt(string id)
        {
            owner = GetComponent<TrainNarrative>();
            var defense = owner.State.OpenDoubt(id); if (defense == null) return false;
            Defense = defense; SelectedEntry = null; palettePage = 0; inspection = "";
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
        void Button(string text, Action action, float x, float y, float width = 170, bool enabled = true)
        {
            var rect = Panel(page, text, new Color(.15f, .25f, .27f), new Vector2(x, y), new Vector2(width, 34));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.interactable = enabled;
            button.onClick.AddListener(() => action());
            var label = Label(rect, text, new Vector2(6, 2), new Vector2(width - 12, 30)); label.alignment = TextAnchor.MiddleCenter;
        }
        public bool DetailsVisible => showDetails;
        public bool Running => Defense != null && Defense.Draft.started && !Defense.Draft.finished;
        bool showDetails;
        float autoTime;
        public void StartDefense()
        {
            if (Defense == null || !Defense.Start()) { DrawBoard(); return; }
            autoTime = 0; showDetails = false; DrawBoard();
        }
        void ResetDefense() { Defense.Reset(); autoTime = 0; showDetails = false; DrawBoard(); }
        void DrawBoard()
        {
            Defense.RefreshPressure();
            Clear();
            Label(page, "DEFEND YOUR QUESTION", new Vector2(22, 16), new Vector2(900, 34), 25);
            Button(ReviewingAccusation ? "Return to review" : "Notebook", Close, 970, 16, 188);
            Label(page, Defense.Interpretation, new Vector2(22, 57), new Vector2(1120, 52), 20);
            Label(page, "Focus " + Defense.FocusUsed + " / " + owner.State.FocusCapacity + "   •   Doubt " + (Defense.Draft.finished ? owner.State.DoubtPressure : Defense.Draft.pressure) + "   •   " + Defense.Draft.waves + " wave(s)" + (Running ? "   •   Running" : Defense.Draft.finished ? "   •   Complete" : ""), new Vector2(22, 115), new Vector2(1120, 26), 20);
            var boardRect = Panel(page, "Defense board", new Color(.065f, .13f, .15f), new Vector2(22, 154), new Vector2(760, 540));
            boardRect.gameObject.AddComponent<RectMask2D>();
            var surface = new GameObject("Board geometry", typeof(RectTransform), typeof(DoubtBoardGraphic)); surface.transform.SetParent(boardRect, false);
            var surfaceRect = surface.GetComponent<RectTransform>(); surfaceRect.anchorMin = Vector2.zero; surfaceRect.anchorMax = Vector2.one; surfaceRect.offsetMin = surfaceRect.offsetMax = Vector2.zero;
            var graphic = surface.GetComponent<DoubtBoardGraphic>(); graphic.view = this; graphic.raycastTarget = true;
            foreach (var tower in Defense.Draft.towers)
            {
                string letter = DoubtRules.Kind(owner.catalogue, tower.entryId) == FocusTowerKind.Projectile ? "C" : DoubtRules.Kind(owner.catalogue, tower.entryId) == FocusTowerKind.Slow ? "S" : "Q";
                Label(boardRect, letter, new Vector2((tower.x + .5f) * 760 / 17 - 6, (tower.y + .5f) * 540 / 13 - 10), new Vector2(18, 20), 15);
            }
            string status = Defense.Draft.finished ? (Defense.Draft.defended ? "Doubt cleared. Affected questions are ready for suspects." : "Defense failed. Questions remain insecure; rearrange and retry.") :
                "Shots • Slow • Buff  |  Red: enemy haste  |  Amber: tower weakness";
            Label(page, status, new Vector2(22, 704), new Vector2(760, 28), 17);
            previewText = Label(page, Defense.Draft.finished ? "New contradictions may raise doubt again. Defense is readiness to question, not proof." : Defense.Message ?? "This run handles accumulated doubt together. Place towers, then test defense.", new Vector2(22, 738), new Vector2(760, 30), 16);
            Button(Defense.Planning ? "Test defense" : Defense.Draft.finished ? "Back to reasoning" : "Defending…", () => { if (Defense.Planning) StartDefense(); else BackToReasoning(); }, 22, 774, 230, !Running);
            Button(Running ? "Stop and rearrange" : "Rearrange", ResetDefense, 264, 774, 210, !Defense.Planning && owner.State.DoubtPressure > 0);
            DrawPalette();
            if (showDetails) DrawDetails();
        }
        void DrawPalette()
        {
            Label(page, "TOWERS", new Vector2(804, 154), new Vector2(354, 30), 23);
            var entries = owner.State.progress.clues.Concat(owner.State.progress.statements).Concat(owner.State.progress.theories).ToList();
            int pages = Mathf.Max(1, Mathf.CeilToInt(entries.Count / 6f)); palettePage = Mathf.Clamp(palettePage, 0, pages - 1);
            for (int i = 0; i < 6 && palettePage * 6 + i < entries.Count; i++)
            {
                string id = entries[palettePage * 6 + i]; var kind = DoubtRules.Kind(owner.catalogue, id);
                string role = kind == FocusTowerKind.Projectile ? "Shot" : kind == FocusTowerKind.Slow ? "Slow" : "Buff";
                Button((SelectedEntry == id ? "• " : "") + id + "  /  " + role + "  /  " + DoubtRules.Cost(owner.catalogue, id) + " focus", () => SelectEntry(id), 804, 194 + i * 44, 354, !Running);
            }
            Button("Previous", () => { palettePage--; DrawBoard(); }, 804, 466, 126, palettePage > 0 && !Running);
            Button("Next", () => { palettePage++; DrawBoard(); }, 940, 466, 126, palettePage < pages - 1 && !Running);
            Label(page, (palettePage + 1) + "/" + pages, new Vector2(1080, 470), new Vector2(78, 26));
            string description = "Select a tower to see its effect.\nPlacement and rearrangement are free.";
            if (SelectedEntry != null)
            {
                var kind = DoubtRules.Kind(owner.catalogue, SelectedEntry); var t = Defense.Tuning;
                description = kind == FocusTowerKind.Projectile ? "Shoots nearby doubts.\nRange " + t.projectileRange + " • Damage " + t.projectileDamage :
                    kind == FocusTowerKind.Slow ? "Slows nearby doubts.\nRange " + t.slowRange + " • Speed ×" + t.slowedSpeed :
                    "Boosts nearby towers.\nRange " + t.buffRange + " • Strength ×" + t.buffMultiplier;
                description += "\nFocus cost: " + DoubtRules.Cost(owner.catalogue, SelectedEntry);
                var placed = Defense.Draft.towers.Find(tower => tower.entryId == SelectedEntry);
                if (placed != null && Defense.TowerEnvironmentMultiplier(placed.Cell) < 1) description += "\nWeakened here: ×" + Defense.TowerEnvironmentMultiplier(placed.Cell);
            }
            Label(page, description, new Vector2(804, 520), new Vector2(354, 115), 19);
            Button("Details", () => { showDetails = true; DrawBoard(); }, 804, 650, 168);
            Button("Remove tower", () => { if (SelectedEntry != null) Defense.Remove(SelectedEntry); DrawBoard(); }, 990, 650, 168, Defense.Planning && SelectedEntry != null);
            Label(page, "Focus limits active towers.\nYour notebook questions are kept.", new Vector2(804, 710), new Vector2(354, 60), 16);
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
        public void SelectEntry(string id) { SelectedEntry = id; inspection = ""; DrawBoard(); }
        public bool PlaceSelected(float x, float y)
        {
            bool result = SelectedEntry != null && Defense.Place(SelectedEntry, new Vector2(x, y)); DrawBoard(); return result;
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
            else { if (root != null) root.SetActive(false); ReviewingAccusation = false; Defense = null; }
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
            { autoTime -= Defense.Tuning.stepSeconds; Defense.Step(); }
            if (count == 0) return;
            if (Defense.Draft.finished) { showDetails = false; autoTime = 0; DrawBoard(); }
            else
            {
                if (previewText != null) previewText.text = Defense.Message;
                foreach (var graphic in root.GetComponentsInChildren<DoubtBoardGraphic>()) graphic.SetVerticesDirty();
            }
        }
        void OnDestroy() { if (root != null) Destroy(root); }
    }
}
