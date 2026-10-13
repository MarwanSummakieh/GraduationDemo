using System;
using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace GraduationDemo.Train
{
    [DefaultExecutionOrder(100)]
    public sealed class InvestigationNotebook : MonoBehaviour
    {
        public enum NotebookTab { Map, Reasoning, Train }
        public NotebookTab Tab { get; private set; } = NotebookTab.Reasoning;
        public ReasoningGraphLayout Graph { get; private set; }
        public int VisibleNodeCount => nodeRects.Count;
        public Vector2 JourneyMarker { get; private set; }
        public int JourneyConnection { get; private set; }
        public ReasoningAssistance Assistance { get; private set; } = ReasoningAssistance.Subtle;
        public List<ReasoningHint> VisibleHints { get; private set; } = new List<ReasoningHint>();
        public void SetAssistance(ReasoningAssistance value) { Assistance = value; refresh = true; HideTooltip(); }
        public bool AccusationGraphActive => accusationMode && owner != null && owner.State.progress.phase == InvestigationPhase.Accusation;
        public IReadOnlyList<string> AccusationTheories => accusation;
        readonly Dictionary<string, string> accusationSockets = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> AccusationSockets => accusationSockets;
        public bool ConversationGraphActive => !string.IsNullOrEmpty(conversationSuspect);
        public string ConversationSuspect => conversationSuspect;
        string conversationSuspect;
        public bool HintConnectionVisible(ReasoningHint hint) => ReasoningGuidance.ConnectionUnbuilt(hint, owner.catalogue, owner.State.progress);
        public bool BeginConversationGraph(string suspect)
        {
            if (!owner.State.InvestigationOpen || !NarrativeCatalogue.IsSuspect(suspect)) return false;
            conversationSuspect = suspect; accusationMode = false; removedMode = false; Tab = NotebookTab.Reasoning;
            search = ""; suspectFilter = ""; selectedTheory = null; selectedClue = null; selectedStatement = null;
            firstFit = true; refresh = true; return true;
        }
        public string AccusationSuspect => accusationSuspect;
        public AccusationAssessment AccusationSupport => owner.State.AssessAccusation(accusationSuspect, accusation, accusationSockets);
        public bool CanSubmitAccusation => AccusationGraphActive && accusation.Count == 3 && accusation.All(owner.State.progress.theories.Contains) && NarrativeCatalogue.IsSuspect(accusationSuspect);
        TrainNarrative owner;
        TrainTour tour;
        Canvas canvas;
        RectTransform window, page, sidebar, viewport, graphContent, tooltip;
        Text detail, status, tooltipText, journeyText;
        Font font;
        InputField searchInput;
        string search = "", suspectFilter = "", selectedClue, selectedStatement, selectedTheory, selectedCar = "odette";
        readonly List<string> accusation = new List<string>();
        bool refresh, firstFit = true, accusationMode, removedMode;
        string pendingAccusationTheory, accusationSuspect, selectedSpecialNode, tetherMessage, accusationFeedback;
        bool awaitingSuspect;
        InvestigationPhase observedPhase;
        Vector2 savedPan;
        float savedZoom = 1;
        bool preserveView;
        string trainOccupancy;
        readonly Dictionary<string, RectTransform> nodeRects = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        NotebookStrokeGraphic strokes;
        NotebookPanZoom navigation;
        GameObject eventSystemCreated;
        readonly Color ink = new Color(.055f, .10f, .105f, .99f), gold = new Color(.95f, .85f, .62f), paper = new Color(.84f, .85f, .77f);
        public static readonly string[] LocationIds = { "odette", "sleeping_a", "bar", "dining", "burrows" };
        public static readonly string[] LocationNames = { "Odette's compartment", "Felix / Mila", "Bar and lounge", "Dining saloon", "Burrows's compartment" };

        void Start()
        {
            owner = GetComponent<TrainNarrative>(); tour = GetComponent<TrainTour>();
            Assistance = owner.configuration == null ? ReasoningAssistance.Subtle : owner.configuration.reasoningAssistance;
            if (owner.State == null) { enabled = false; return; }
            observedPhase = owner.State.progress.phase; accusationMode = observedPhase == InvestigationPhase.Accusation;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var root = new GameObject("Investigation notebook canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            if (EventSystem.current == null)
            {
                eventSystemCreated = new GameObject("Investigation UI events", typeof(EventSystem), typeof(InputSystemUIInputModule));
                eventSystemCreated.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            window = Panel(canvas.transform, "Notebook", ink); Stretch(window, 20, 20, -20, -20);
            Label(window, "Basil Burrows's notebook", 24, gold, new Vector2(22, -18), new Vector2(400, 34));
            var close = Button(window, "Close", () => owner.ToggleNotebook(), new Vector2(-22, -18), new Vector2(80, 34)); close.anchorMin = close.anchorMax = new Vector2(1, 1); close.pivot = new Vector2(1, 1);
            Button(window, "MAP", () => SelectTab(NotebookTab.Map), new Vector2(22, -62), new Vector2(145, 34));
            Button(window, "REASONING", () => SelectTab(NotebookTab.Reasoning), new Vector2(179, -62), new Vector2(145, 34));
            Button(window, "TRAIN", () => SelectTab(NotebookTab.Train), new Vector2(336, -62), new Vector2(145, 34));
            page = Panel(window, "Page", new Color(.075f, .14f, .15f)); Stretch(page, 16, 18, -16, -112);
            tooltip = Panel(window, "Hover detail", new Color(.12f, .20f, .21f, 1)); tooltip.sizeDelta = new Vector2(380, 140); tooltip.anchorMin = tooltip.anchorMax = new Vector2(.5f, .5f); tooltip.pivot = new Vector2(0, 1);
            tooltip.GetComponent<Image>().raycastTarget = false;
            tooltipText = Label(tooltip, "", 18, gold, new Vector2(12, -10), new Vector2(356, 120)); tooltip.gameObject.SetActive(false);
            owner.State.Changed += ScheduleRefresh; window.gameObject.SetActive(false);
        }
        void ScheduleRefresh()
        {
            if (observedPhase != owner.State.progress.phase)
            {
                observedPhase = owner.State.progress.phase;
                if (observedPhase == InvestigationPhase.Accusation) BeginAccusationGraph();
                else if (observedPhase == InvestigationPhase.Resolved) accusationMode = false;
            }
            refresh = true;
        }
        public bool BeginAccusationGraph()
        {
            if (owner == null || owner.State.progress.phase != InvestigationPhase.Accusation) return false;
            accusationMode = true; removedMode = false; Tab = NotebookTab.Reasoning; search = ""; suspectFilter = "";
            conversationSuspect = null;
            firstFit = true; refresh = true; pendingAccusationTheory = null; selectedSpecialNode = null;
            selectedClue = null; selectedStatement = null; selectedTheory = null;
            tetherMessage = "Click a theory, then a Means, Motive or Opportunity socket."; return true;
        }
        public void SetOpen(bool open)
        {
            if (window == null) return;
            if (!open) conversationSuspect = null;
            window.gameObject.SetActive(open); HideTooltip();
            if (open) { refresh = true; firstFit = true; }
        }
        public void SelectTab(NotebookTab tab) { if (tab != NotebookTab.Reasoning) conversationSuspect = null; Tab = tab; firstFit = true; refresh = true; }
        void LateUpdate()
        {
            if (canvas == null) return;
            canvas.scaleFactor = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .75f, 1.25f);
            if (!window.gameObject.activeSelf) return;
            if (Tab == NotebookTab.Train)
            {
                string occupancy = string.Join("|", FindObjectsByType<TrainActor>().Select(a => a.characterId + ":" + ClosestLocation(a.transform.position.x)).OrderBy(s => s));
                if (trainOccupancy != occupancy) { trainOccupancy = occupancy; refresh = true; }
            }
            if (refresh) { refresh = false; Rebuild(); }
            if (Tab == NotebookTab.Reasoning && Graph != null && !removedMode)
            {
                foreach (var node in Graph.nodes)
                {
                    if (!nodeRects.TryGetValue(node.id, out var rect)) continue;
                    Vector2 target = new Vector2(node.position.x, -node.position.y);
                    rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 9));
                    positions[node.id] = rect.anchoredPosition;
                }
                strokes?.SetVerticesDirty();
            }
            if (status != null) status.text = AccusationGraphActive ? tetherMessage ?? accusationFeedback ?? "Click a theory, then the accusation node." : owner.Message ?? "Select a clue and statement to make a theory.";
        }
        void Rebuild()
        {
            accusation.RemoveAll(id => !owner.State.progress.theories.Contains(id));
            foreach (var role in accusationSockets.Keys.ToList()) if (!accusation.Contains(accusationSockets[role])) accusationSockets.Remove(role);
            if (accusation.Count < 3) { accusationSuspect = null; awaitingSuspect = false; }
            if (pendingAccusationTheory != null && !owner.State.progress.theories.Contains(pendingAccusationTheory)) pendingAccusationTheory = null;
            if (selectedTheory != null && !owner.State.progress.theories.Contains(selectedTheory)) selectedTheory = null;
            preserveView = !firstFit && Tab == NotebookTab.Reasoning && navigation != null;
            if (preserveView) { savedPan = navigation.content.anchoredPosition; savedZoom = navigation.zoom; }
            HideTooltip(); foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            nodeRects.Clear(); detail = null; strokes = null; searchInput = null;
            Canvas.ForceUpdateCanvases();
            if (Tab == NotebookTab.Map) BuildMap();
            else if (Tab == NotebookTab.Train) BuildTrain();
            else BuildReasoning();
            tooltip.SetAsLastSibling();
        }
        RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color;
            return go.GetComponent<RectTransform>();
        }
        static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(right, top); }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        { rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; }
        Text Label(Transform parent, string text, int size, Color color, Vector2 position, Vector2 dimensions)
        {
            var go = new GameObject("Text / " + text, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var value = go.GetComponent<Text>(); value.font = font; value.fontSize = size; value.color = color; value.text = text; value.raycastTarget = false;
            value.horizontalOverflow = HorizontalWrapMode.Wrap; value.verticalOverflow = VerticalWrapMode.Truncate;
            Place(go.GetComponent<RectTransform>(), position, dimensions); return value;
        }
        RectTransform Button(Transform parent, string text, Action action, Vector2 position, Vector2 size)
        {
            var rect = Panel(parent, text, new Color(.17f, .26f, .27f)); Place(rect, position, size);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = rect.GetComponent<Image>(); button.onClick.AddListener(() => action());
            var label = Label(rect, text, 17, gold, Vector2.zero, size); Stretch(label.rectTransform, 8, 3, -8, -3); label.alignment = TextAnchor.MiddleCenter;
            return rect;
        }
        RectTransform ScrollContent(Transform parent, Vector2 position, Vector2 size, float contentHeight, bool horizontal = false, float contentWidth = 0)
        {
            var view = Panel(parent, "Scrollable page", new Color(.07f, .12f, .13f)); Place(view, position, size); view.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>(); content.SetParent(view, false); Place(content, Vector2.zero, new Vector2(contentWidth <= 0 ? size.x : contentWidth, contentHeight));
            var scroll = view.gameObject.AddComponent<ScrollRect>(); scroll.content = content; scroll.viewport = view; scroll.horizontal = horizontal; scroll.vertical = !horizontal; scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }
        void BuildReasoning()
        {
            if (removedMode) { BuildRemovedTheories(); return; }
            if (owner.State.progress.phase != InvestigationPhase.Accusation) accusationMode = false;
            float width = page.rect.width, height = page.rect.height; bool narrow = width < 880;
            viewport = Panel(page, "Reasoning graph viewport", new Color(.065f, .12f, .13f));
            Place(viewport, new Vector2(8, -52), new Vector2(narrow ? width - 16 : width - 300, narrow ? height - (ConversationGraphActive ? 281 : 245) : height - 64)); viewport.gameObject.AddComponent<RectMask2D>();
            graphContent = new GameObject("Graph", typeof(RectTransform)).GetComponent<RectTransform>(); graphContent.SetParent(viewport, false); graphContent.anchorMin = graphContent.anchorMax = new Vector2(.5f, .5f); graphContent.pivot = new Vector2(0, 1);
            Graph = ConversationGraphActive ? ConversationGraphLayout.Build(owner.State, conversationSuspect, search) : AccusationGraphActive ? AccusationGraphLayout.Build(owner.catalogue, owner.State.progress, accusation, accusationSuspect, search, suspectFilter, accusationSockets) :
                ReasoningGraphLayout.Build(owner.catalogue, owner.State.progress, search, suspectFilter); graphContent.sizeDelta = Graph.size;
            VisibleHints = ReasoningGuidance.Available(owner.configuration, owner.State.progress, Assistance)
                .Where(h => Graph.nodes.Any(n => n.id == h.firstId) && Graph.nodes.Any(n => n.id == h.secondId)).ToList();
            var edgeObject = new GameObject("Parent connections", typeof(RectTransform), typeof(NotebookStrokeGraphic)); edgeObject.transform.SetParent(graphContent, false);
            var edgeRect = edgeObject.GetComponent<RectTransform>(); Stretch(edgeRect, 0, 0, 0, 0); edgeRect.pivot = new Vector2(0, 1);
            strokes = edgeObject.GetComponent<NotebookStrokeGraphic>(); strokes.notebook = this; strokes.raycastTarget = false;
            navigation = viewport.gameObject.AddComponent<NotebookPanZoom>(); navigation.content = graphContent; navigation.notebook = this;
            foreach (var node in Graph.nodes) AddNode(node);
            // Layout groups remain constrained to clues / authored theories / suspect statements.
            Color sourceHeading = AccusationGraphActive || ConversationGraphActive ? new Color(paper.r, paper.g, paper.b, .35f) : gold;
            Label(graphContent, "CLUES", 24, sourceHeading, new Vector2(40, -8), new Vector2(190, 35));
            Label(graphContent, "THEORIES", 24, gold, new Vector2(380, -8), new Vector2(260, 35));
            Label(graphContent, "STATEMENTS", 24, sourceHeading, new Vector2(920, -8), new Vector2(230, 35));
            if (AccusationGraphActive)
            {
                Label(graphContent, "SOCKETS", 24, gold, new Vector2(1120, -8), new Vector2(180, 35));
                Label(graphContent, "ACCUSATION", 24, gold, new Vector2(1330, -8), new Vector2(220, 35));
                Label(graphContent, "SUSPECTS", 24, gold, new Vector2(1610, -8), new Vector2(220, 35));
            }
            foreach (var speaker in new[] { "Felix", "Mila", "Bellamy" })
            {
                var first = Graph.nodes.FirstOrDefault(n => n.kind == ReasoningNodeKind.Statement && n.speaker == speaker);
                if (first != null) Label(graphContent, speaker, 20, AccusationGraphActive ? sourceHeading : paper, new Vector2(1090, -first.position.y - 8), new Vector2(70, 55));
            }
            Canvas.ForceUpdateCanvases();
            if (preserveView) { navigation.zoom = savedZoom; graphContent.localScale = Vector3.one * savedZoom; graphContent.anchoredPosition = savedPan; }
            else
            {
                navigation.Fit(Graph.size);
                // Start with usable hit targets; Fit remains an explicit overview for a large graph.
                if (!AccusationGraphActive && navigation.zoom < .45f)
                {
                    navigation.zoom = .45f; graphContent.localScale = Vector3.one * .45f;
                    graphContent.anchoredPosition = new Vector2(-Graph.size.x * .45f * .5f, viewport.rect.height * .5f - 35);
                }
            }
            firstFit = false;
            AddSearch();
            var filterButton = Button(page, ConversationGraphActive ? conversationSuspect : string.IsNullOrEmpty(suspectFilter) ? "All" : suspectFilter, () => { suspectFilter = suspectFilter == "" ? "Felix" : suspectFilter == "Felix" ? "Mila" : suspectFilter == "Mila" ? "Bellamy" : ""; firstFit = true; refresh = true; }, new Vector2(248, -8), new Vector2(90, 32));
            filterButton.GetComponent<Button>().interactable = !ConversationGraphActive;
            Button(page, "Fit", () => navigation.Fit(Graph.size), new Vector2(346, -8), new Vector2(70, 32));
            Button(page, "Removed: " + owner.State.progress.removedTheories.Count, () => { removedMode = true; refresh = true; }, new Vector2(424, -8), new Vector2(104, 32));
            var guidance = Button(page, "Guidance: " + Assistance, () => SetAssistance((ReasoningAssistance)(((int)Assistance + 1) % 4)), new Vector2(536, -8), new Vector2(180, 32));
            var guidanceHover = guidance.gameObject.AddComponent<NotebookHover>(); guidanceHover.notebook = this;
            guidanceHover.text = "Click to cycle Off / Subtle / Guided / Directed.\nGuided adds links and questions; Directed also names whom to approach.\n? Interview lead; + case-building connection.\nCase connections do not suggest another interview.";
            if (owner.State.progress.phase == InvestigationPhase.Accusation)
                Button(page, AccusationGraphActive ? "Reasoning" : "Accusation", () => { if (AccusationGraphActive) { accusationMode = false; firstFit = true; refresh = true; } else BeginAccusationGraph(); }, new Vector2(724, -8), new Vector2(130, 32));
            sidebar = Panel(page, "Theory detail", ink);
            Place(sidebar, new Vector2(narrow ? 8 : width - 280, narrow ? -(height - (ConversationGraphActive ? 217 : 181)) : -52), new Vector2(narrow ? width - 16 : 272, narrow ? (ConversationGraphActive ? 207 : 171) : height - 64));
            float detailHeight = narrow ? 66 : sidebar.rect.height - 172;
            var detailContent = ScrollContent(sidebar, new Vector2(10, -8), new Vector2(sidebar.rect.width - 20, detailHeight), 400);
            detail = Label(detailContent, SelectedDetail(), 18, gold, Vector2.zero, new Vector2(sidebar.rect.width - 28, 400));
            float textHeight = Mathf.Max(detailHeight, detail.preferredHeight + 16); detailContent.sizeDelta = new Vector2(detailContent.sizeDelta.x, textHeight); detail.rectTransform.sizeDelta = new Vector2(detail.rectTransform.sizeDelta.x, textHeight);
            if (owner.State.progress.phase == InvestigationPhase.Accusation)
            {
                var button = Button(sidebar, AccusationGraphActive ? (accusationSuspect == null ? "Tether a suspect to confirm" : "Confirm accusation: " + accusationSuspect) : "Build accusation graph",
                    () => { if (AccusationGraphActive) SubmitAccusation(); else BeginAccusationGraph(); }, new Vector2(10, narrow ? -79 : -(sidebar.rect.height - 108)), new Vector2(sidebar.rect.width - 20, 32));
                button.GetComponent<Button>().interactable = !AccusationGraphActive || CanSubmitAccusation;
            }
            else if (ConversationGraphActive)
            {
                string target = conversationSuspect;
                var alibi = owner.State.Alibi(target);
                var ask = Button(sidebar, "Ask " + target + " for an alibi", () => owner.OfferConversation(owner.State.Alibi(target)), new Vector2(10, narrow ? -79 : -(sidebar.rect.height - 160)), new Vector2(sidebar.rect.width - 20, 32));
                ask.GetComponent<Button>().interactable = alibi != null && alibi.cost <= owner.State.RemainingActions;
                var present = Button(sidebar, "Present theory to " + target, () => owner.Confront(selectedTheory, target), new Vector2(10, narrow ? -115 : -(sidebar.rect.height - 124)), new Vector2(sidebar.rect.width - 20, 32));
                var prompt = selectedTheory == null ? null : owner.State.Presentation(selectedTheory, target);
                present.GetComponent<Button>().interactable = prompt != null && prompt.cost <= owner.State.RemainingActions;
            }
            else if (owner.State.InvestigationOpen)
                for (int i = 0; i < 3; i++)
                {
                    string target = new[] { "Felix", "Mila", "Bellamy" }[i];
                    var rect = Button(sidebar, "Present to " + target, () => owner.Confront(selectedTheory, target), new Vector2(narrow ? 10 + i * (sidebar.rect.width - 20) / 3 : 10, narrow ? -79 : -(sidebar.rect.height - 160 + i * 36)), new Vector2(narrow ? (sidebar.rect.width - 26) / 3 : sidebar.rect.width - 20, 32));
                    rect.GetComponent<Button>().interactable = selectedTheory != null && owner.State.Presentation(selectedTheory, target) != null;
                    if (Assistance == ReasoningAssistance.Directed && ReasoningGuidance.Available(owner.configuration, owner.State.progress, Assistance)
                        .Any(h => h.purpose == ReasoningHintPurpose.Interview && h.recipient == target && ReasoningGuidance.Matches(h, owner.catalogue.Theory(selectedTheory))))
                        rect.GetComponent<Image>().color = new Color(.22f, .40f, .35f);
                }
            status = Label(sidebar, owner.Message ?? "Click a clue, then a statement. Drag to pan; scroll to zoom.", 16, paper, new Vector2(10, -(sidebar.rect.height - 52)), new Vector2(sidebar.rect.width - 20, 48));
        }
        void AddSearch()
        {
            var rect = Panel(page, "Search graph", new Color(.15f, .23f, .24f)); Place(rect, new Vector2(8, -8), new Vector2(230, 32));
            searchInput = rect.gameObject.AddComponent<InputField>();
            var text = Label(rect, "", 17, gold, new Vector2(8, -3), new Vector2(214, 26)); searchInput.textComponent = text;
            var placeholder = Label(rect, "Search discoveries / theories", 15, paper, new Vector2(8, -3), new Vector2(214, 26)); searchInput.placeholder = placeholder;
            searchInput.SetTextWithoutNotify(search); searchInput.onEndEdit.AddListener(value => { if (search == value) return; search = value; firstFit = true; refresh = true; });
        }
        void AddNode(ReasoningNode node)
        {
            var go = new GameObject(node.id, typeof(RectTransform), typeof(NotebookNodeGraphic), typeof(Button), typeof(NotebookHover)); go.transform.SetParent(graphContent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(60, 60);
            bool background = (AccusationGraphActive || ConversationGraphActive) && (node.kind == ReasoningNodeKind.Clue || node.kind == ReasoningNodeKind.Statement);
            if (background) rect.sizeDelta = new Vector2(48, 48);
            if (AccusationGraphActive && node.kind == ReasoningNodeKind.Theory) rect.sizeDelta = new Vector2(72, 72);
            if (node.kind == ReasoningNodeKind.Accusation) rect.sizeDelta = new Vector2(100, 100);
            if (node.kind == ReasoningNodeKind.Suspect) rect.sizeDelta = new Vector2(84, 84);
            if (node.kind == ReasoningNodeKind.AccusationSocket) rect.sizeDelta = new Vector2(72, 72);
            rect.anchoredPosition = positions.TryGetValue(node.id, out var prior) ? prior : new Vector2(node.position.x, -node.position.y);
            nodeRects.Add(node.id, rect); positions[node.id] = rect.anchoredPosition;
            var graphic = go.GetComponent<NotebookNodeGraphic>(); graphic.kind = node.kind;
            graphic.color = node.kind == ReasoningNodeKind.Clue ? gold : node.speaker == "Mila" ? new Color(.38f, .67f, .84f) : node.speaker == "Felix" ? new Color(.84f, .64f, .37f) : new Color(.45f, .74f, .64f);
            if (node.id == selectedClue || node.id == selectedStatement || node.id == selectedTheory) graphic.color = Color.Lerp(graphic.color, Color.white, .45f);
            if (AccusationGraphActive && node.kind == ReasoningNodeKind.Theory) graphic.color = accusation.Contains(node.id) ? new Color(1, .90f, .52f) : gold;
            if (node.kind == ReasoningNodeKind.Accusation) graphic.color = gold;
            if (node.kind == ReasoningNodeKind.AccusationSocket) graphic.color = accusationSockets.ContainsKey(node.speaker) ? gold : new Color(.55f, .63f, .62f);
            if (node.kind == ReasoningNodeKind.AccusationSocket && pendingAccusationTheory != null && owner.catalogue.Theory(pendingAccusationTheory)?.propositions.Contains(node.speaker) == true)
                graphic.color = new Color(.45f, .82f, .65f);
            if (node.kind == ReasoningNodeKind.Suspect && node.speaker == accusationSuspect) graphic.color = gold;
            if (background) graphic.color = ConversationGraphActive ? new Color(.55f, .55f, .55f, .30f) : new Color(graphic.color.r, graphic.color.g, graphic.color.b, .30f);
            go.GetComponent<Button>().targetGraphic = graphic; go.GetComponent<Button>().onClick.AddListener(() => SelectNode(node.id));
            if (ConversationGraphActive && node.kind != ReasoningNodeKind.Theory) go.GetComponent<Button>().interactable = false;
            var hover = go.GetComponent<NotebookHover>(); hover.notebook = this; hover.text = node.label + "\n" + node.detail;
            if (node.kind == ReasoningNodeKind.Accusation) hover.text = AccusationSupport.DisplayText + "\n\n" + hover.text;
            if (node.kind == ReasoningNodeKind.Theory && AccusationGraphActive)
                hover.text = "Click this theory, then a Means, Motive or Opportunity socket. Repeat to untether it.\nSupports: " + string.Join(", ", owner.catalogue.Theory(node.id).propositions.Where(AccusationReasoning.Roles.Contains).Select(AccusationReasoning.Label)) + "\n\n" + hover.text;
            else if (node.kind == ReasoningNodeKind.Theory && Assistance == ReasoningAssistance.Directed)
                hover.text = DirectedActionSummary(node.id) + "\n\n" + hover.text;
            if (node.kind == ReasoningNodeKind.Theory)
            {
                hover.theoryId = node.id;
                hover.text += "\n\nRight-click to remove theory. Restore it from Removed.";
            }
            var hints = node.kind == ReasoningNodeKind.Theory ? VisibleHints.Where(h => ReasoningGuidance.Matches(h, owner.catalogue.Theory(node.id))).ToList() :
                VisibleHints.Where(h => h.firstId == node.id || h.secondId == node.id).ToList();
            if (hints.Count > 0)
            {
                hover.text += "\n\n" + string.Join("\n\n", hints.Select(h => ReasoningGuidance.Text(h, Assistance)));
                string badge = (hints.Any(h => h.kind == ReasoningHintKind.Contradiction) ? "!" : "") +
                    (hints.Any(h => h.kind == ReasoningHintKind.Ambiguity) ? "?" : "") +
                    (hints.Any(h => h.kind == ReasoningHintKind.CaseConnection) ? "+" : "");
                Label(rect, badge, 24, new Color(1, .68f, .37f, background ? .30f : 1), new Vector2(46, 8), new Vector2(38, 32));
            }
            string icon = node.kind == ReasoningNodeKind.AccusationSocket ? (node.speaker == AccusationReasoning.Opportunity ? "OP" : node.speaker == AccusationReasoning.Means ? "ME" : "MO") : node.kind == ReasoningNodeKind.Accusation ? "CASE\n" + accusation.Count + "/3" : node.kind == ReasoningNodeKind.Suspect ? node.speaker.Substring(0, 1) :
                node.kind == ReasoningNodeKind.Theory ? node.label.Replace("Theory ", "") : node.kind == ReasoningNodeKind.Clue ? node.id : node.speaker.Substring(0, 1) + node.id.Substring(node.id.Length - 2);
            var inside = Label(rect, icon, 18, background ? new Color(ink.r, ink.g, ink.b, .4f) : ink, Vector2.zero, rect.sizeDelta); Stretch(inside.rectTransform, 0, 0, 0, 0); inside.alignment = TextAnchor.MiddleCenter;
            float captionWidth = node.kind == ReasoningNodeKind.Theory ? 76 : 180;
            var caption = Label(rect, node.label, AccusationGraphActive && node.kind == ReasoningNodeKind.Theory ? 14 : 17, background ? new Color(paper.r, paper.g, paper.b, .38f) : paper, new Vector2((rect.sizeDelta.x - captionWidth) / 2, -rect.sizeDelta.y - 2), new Vector2(captionWidth, 45)); caption.alignment = TextAnchor.UpperCenter;
            caption.raycastTarget = true;
        }
        public void SelectNode(string id)
        {
            var node = Graph?.nodes.Find(n => n.id == id); if (node == null) return;
            if (ConversationGraphActive && node.kind != ReasoningNodeKind.Theory) return;
            if (node.kind == ReasoningNodeKind.AccusationSocket)
            {
                selectedSpecialNode = id;
                if (pendingAccusationTheory != null) TetherTheoryToSocket(pendingAccusationTheory, node.speaker);
                else { tetherMessage = "Select a theory, then this " + AccusationReasoning.Label(node.speaker) + " socket."; refresh = true; }
                return;
            }
            if (node.kind == ReasoningNodeKind.Accusation)
            {
                selectedSpecialNode = id;
                if (pendingAccusationTheory != null) TetherTheoryToAccusation(pendingAccusationTheory);
                else { awaitingSuspect = accusation.Count == 3; tetherMessage = awaitingSuspect ? (accusationSuspect == null ? "Click a suspect node to tether the accusation." : "Change the suspect tether, or confirm your accusation.") : "Tether three theories before choosing a suspect."; refresh = true; }
                return;
            }
            if (node.kind == ReasoningNodeKind.Suspect)
            {
                selectedSpecialNode = id;
                if (awaitingSuspect) TetherAccusationToSuspect(node.speaker);
                else { tetherMessage = "Click the accusation node, then a suspect to tether."; refresh = true; }
                return;
            }
            selectedSpecialNode = null; accusationFeedback = null;
            if (node.kind == ReasoningNodeKind.Clue || node.kind == ReasoningNodeKind.Statement)
            {
                selectedTheory = null; pendingAccusationTheory = null; awaitingSuspect = false;
                if (AccusationGraphActive) tetherMessage = node.kind == ReasoningNodeKind.Clue ? "Choose a statement to construct a theory." : "Choose a clue to construct a theory.";
            }
            if (node.kind == ReasoningNodeKind.Clue) selectedClue = id;
            else if (node.kind == ReasoningNodeKind.Statement) selectedStatement = id;
            else { selectedTheory = id; selectedClue = null; selectedStatement = null; if (AccusationGraphActive) { pendingAccusationTheory = id; awaitingSuspect = false; tetherMessage = "Now click a Means, Motive or Opportunity socket."; } }
            if (node.kind != ReasoningNodeKind.Theory && selectedClue != null && selectedStatement != null && owner.State.CanConstructTheories)
            {
                var theory = owner.State.Construct(selectedClue, selectedStatement);
                if (theory != null) { selectedTheory = theory.id; selectedClue = null; selectedStatement = null; if (AccusationGraphActive) { pendingAccusationTheory = theory.id; tetherMessage = "Theory constructed. Choose its accusation socket."; } }
            }
            refresh = true;
        }
        public bool TetherTheoryToAccusation(string id)
        {
            if (!AccusationGraphActive || !owner.State.progress.theories.Contains(id)) return false;
            var existing = accusationSockets.FirstOrDefault(p => p.Value == id);
            if (existing.Key != null) return TetherTheoryToSocket(id, existing.Key);
            var theory = owner.catalogue.Theory(id);
            var role = AccusationReasoning.Roles.FirstOrDefault(r => !accusationSockets.ContainsKey(r) && theory.propositions.Contains(r)) ?? AccusationReasoning.Roles.FirstOrDefault(r => !accusationSockets.ContainsKey(r));
            return role != null && TetherTheoryToSocket(id, role);
        }
        public bool TetherTheoryToSocket(string id, string role)
        {
            if (!AccusationGraphActive || !owner.State.progress.theories.Contains(id)) return false;
            if (!AccusationReasoning.Roles.Contains(role)) return false;
            if (accusationSockets.TryGetValue(role, out var old) && old == id) { accusationSockets.Remove(role); accusation.Remove(id); accusationSuspect = null; }
            else
            {
                if (old != null) accusation.Remove(old);
                foreach (var key in accusationSockets.Keys.ToList()) if (accusationSockets[key] == id) accusationSockets.Remove(key);
                accusationSockets[role] = id; if (!accusation.Contains(id)) accusation.Add(id);
                accusationSuspect = null;
            }
            pendingAccusationTheory = null; accusationFeedback = null; awaitingSuspect = accusation.Count == 3;
            tetherMessage = awaitingSuspect ? "Click a suspect node to tether your accusation." : "Tethered " + accusation.Count + "/3 theories. Choose another theory.";
            refresh = true; return true;
        }
        public bool TetherAccusationToSuspect(string suspect)
        {
            if (!AccusationGraphActive || accusation.Count != 3 || !NarrativeCatalogue.IsSuspect(suspect)) return false;
            accusationSuspect = accusationSuspect == suspect ? null : suspect; awaitingSuspect = false; accusationFeedback = null;
            tetherMessage = accusationSuspect == null ? "Suspect untethered. Click accusation, then a suspect." : "Review your tethers, then confirm the accusation.";
            refresh = true; return true;
        }
        public bool SubmitAccusation()
        {
            if (!CanSubmitAccusation) return false;
            bool result = owner.AccuseWithSockets(accusationSuspect, accusation, accusationSockets);
            accusationFeedback = result ? null : owner.Message; tetherMessage = null; refresh = true; return result;
        }
        public bool RemoveSelectedTheory()
        {
            return RemoveTheory(selectedTheory);
        }
        public bool RemoveTheory(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (!owner.State.RemoveTheory(id)) return false;
            accusation.Remove(id); positions.Remove(id);
            if (selectedTheory == id) selectedTheory = null;
            HideTooltip();
            refresh = true; return true;
        }
        string SelectedDetail()
        {
            string content = SelectedDetailContent();
            return AccusationGraphActive ? AccusationSupport.DisplayText + "\n\n" + content : content;
        }
        string SelectedDetailContent()
        {
            if (AccusationGraphActive && selectedSpecialNode != null)
                return (accusationFeedback == null ? "" : accusationFeedback + "\n\n") + (Graph.nodes.Find(n => n.id == selectedSpecialNode)?.detail ?? "Build your accusation with tethers.");
            var theory = owner.catalogue.Theory(selectedTheory);
            if (ConversationGraphActive && theory == null) return "Speak to " + conversationSuspect + "\n\nSelect a theory in the graph, then present it. Greyed clues and statements show its sources; hover them for details. You may also ask for an available alibi.\n\nDrag to pan; scroll to zoom.";
            string text = theory != null ? theory.text + "\n\nClue: " + owner.catalogue.Clue(theory.clueId).name + "\nStatement: " + owner.catalogue.Statement(theory.statementId).claim :
                Graph?.nodes.Find(n => n.id == (selectedClue ?? selectedStatement))?.detail ?? "Click a clue and a statement to make a theory. Drag to pan; scroll to zoom. Hover over any icon for its details.";
            if (theory != null && AccusationGraphActive) text = "Choose a Means, Motive or Opportunity socket.\nSupports: " +
                (theory.propositions.Any(AccusationReasoning.Roles.Contains) ? string.Join(", ", theory.propositions.Where(AccusationReasoning.Roles.Contains).Select(AccusationReasoning.Label)) : "Context only") + "\n\n" + text;
            else if (theory != null && Assistance == ReasoningAssistance.Directed) text = DirectedActionSummary(theory.id) + "\n\n" + text;
            if (ConversationGraphActive) text = "Speak to " + conversationSuspect + "\n\n" + text;
            var ids = new[] { selectedClue, selectedStatement };
            var hints = theory != null ? VisibleHints.Where(h => ReasoningGuidance.Matches(h, theory)).ToList() :
                VisibleHints.Where(h => ids.Contains(h.firstId) || ids.Contains(h.secondId)).ToList();
            if (hints.Count > 0) text += "\n\nREASONING NOTES\n" + string.Join("\n\n", hints.Select(h => ReasoningGuidance.Text(h, Assistance)));
            if (AccusationGraphActive && theory == null) text = "Click a clue and statement to make another theory. Click a theory, then a Means, Motive or Opportunity socket. After three theories, tether the accusation to a suspect node and confirm.\n\n" + text;
            else if (Assistance == ReasoningAssistance.Directed)
            {
                if (theory != null && hints.Count == 0) text += "\n\nNo new interview lead is identified for this theory. A reaction may not supply new testimony.";
                else if (theory == null && selectedClue == null && selectedStatement == null) text += "\n\n" + DirectedInstructions();
            }
            return text;
        }
        string DirectedInstructions()
        {
            if (!owner.State.InvestigationOpen) return owner.State.CanConstructTheories ? "Interviews and collection are closed. You can still combine collected clues and statements into new theories. Use the Accusation graph to tether three independent theories and a suspect." : "Case resolved. Review your notebook.";
            return "Ask for each suspect's alibi and explore the train for clues.\n\n? links suggest interviews: combine the linked clue and statement, select the theory, then approach the named suspect.\n\n+ links build your case: review them without spending an interview action. Look for independent evidence of means, motive and opportunity.\n\nWhen ready, end the remaining days. The final accusation opens only after the final day closes.";
        }
        public string DirectedActionSummary(string theoryId)
        {
            var hints = ReasoningGuidance.Available(owner.configuration, owner.State.progress, ReasoningAssistance.Directed)
                .Where(h => ReasoningGuidance.Matches(h, owner.catalogue.Theory(theoryId))).ToList();
            var interview = hints.FirstOrDefault(h => h.purpose == ReasoningHintPurpose.Interview && NarrativeCatalogue.IsSuspect(h.recipient));
            if (interview != null) return "NEXT INTERVIEW: Bring this theory to " + interview.recipient + ".";
            if (hints.Any(h => h.purpose == ReasoningHintPurpose.CaseReview)) return "CASE REVIEW: No further interview is suggested. Keep this connection for your final reasoning.";
            return "NO GUIDED INTERVIEW: No new interview lead is identified for this theory.";
        }
        public Vector2 NodePosition(string id) => positions.TryGetValue(id, out var value) ? value : Vector2.zero;
        public void ShowTooltip(string text, Vector2 screenPosition)
        {
            if (tooltip == null) return; tooltipText.text = text;
            float tooltipHeight = Mathf.Min(window.rect.height - 16, Mathf.Max(140, tooltipText.preferredHeight + 24));
            tooltip.sizeDelta = new Vector2(380, tooltipHeight);
            tooltipText.rectTransform.sizeDelta = new Vector2(356, tooltipHeight - 20);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(window, screenPosition, null, out var local);
            tooltip.anchoredPosition = new Vector2(Mathf.Clamp(local.x + 16, window.rect.xMin + 8, window.rect.xMax - 390), Mathf.Clamp(local.y - 12, window.rect.yMin + tooltipHeight + 8, window.rect.yMax - 8));
            tooltip.gameObject.SetActive(true); tooltip.SetAsLastSibling();
        }
        public void HideTooltip() { if (tooltip != null) tooltip.gameObject.SetActive(false); }
        void BuildMap()
        {
            var mapRect = Panel(page, "European journey", new Color(.095f, .17f, .18f)); Stretch(mapRect, 12, 80, -12, -12);
            var drawing = new GameObject("Journey lines and marker", typeof(RectTransform), typeof(NotebookStrokeGraphic)); drawing.transform.SetParent(mapRect, false);
            Stretch(drawing.GetComponent<RectTransform>(), 0, 0, 0, 0);
            var graphic = drawing.GetComponent<NotebookStrokeGraphic>(); graphic.notebook = this; graphic.map = true; graphic.raycastTarget = false;
            var stops = owner.configuration == null ? new List<JourneyStop> { new JourneyStop { name = "Departure", position = new Vector2(.1f, .2f) }, new JourneyStop { name = "Destination", position = new Vector2(.9f, .8f) } } : owner.configuration.stops;
            Canvas.ForceUpdateCanvases();
            foreach (var stop in stops) Label(mapRect, stop.name, 18, gold, new Vector2(stop.position.x * mapRect.rect.width - 60, -(1 - stop.position.y) * mapRect.rect.height - 20), new Vector2(160, 45));
            journeyText = Label(page, "", 19, gold, new Vector2(14, -(page.rect.height - 68)), new Vector2(page.rect.width - 28, 58));
            int left = owner.State.InvestigationOpen ? Mathf.Max(0, stops.Count - 1 - Mathf.FloorToInt(owner.State.JourneyProgress * (stops.Count - 1))) : 0;
            journeyText.text = owner.State.InvestigationOpen ? "YOU ARE HERE — travelling between stops\n" + left + " stop(s) remaining / " + (owner.State.Days - owner.State.progress.day + 1) + " investigation day(s) remaining" : "Arrived at the destination. Investigation closed.";
        }
        public void DrawMap(VertexHelper vh, Rect bounds)
        {
            Vector2 P(Vector2 v) => new Vector2(bounds.xMin + bounds.width * v.x, bounds.yMin + bounds.height * v.y);
            var coast = new[] { new Vector2(.1f,.35f), new Vector2(.22f,.3f), new Vector2(.28f,.16f), new Vector2(.43f,.17f), new Vector2(.46f,.28f), new Vector2(.6f,.27f), new Vector2(.72f,.15f), new Vector2(.82f,.22f), new Vector2(.75f,.4f), new Vector2(.93f,.5f), new Vector2(.88f,.73f), new Vector2(.63f,.78f), new Vector2(.58f,.95f), new Vector2(.48f,.91f), new Vector2(.52f,.66f), new Vector2(.34f,.68f), new Vector2(.27f,.51f), new Vector2(.1f,.35f) };
            for (int i = 1; i < coast.Length; i++) NotebookStrokeGraphic.Stroke(vh, P(coast[i - 1]), P(coast[i]), new Color(.3f,.43f,.43f), 2);
            NotebookStrokeGraphic.Stroke(vh, P(new Vector2(.16f,.5f)), P(new Vector2(.2f,.75f)), new Color(.3f,.43f,.43f), 9);
            var stops = owner.configuration?.stops; if (stops == null || stops.Count < 2) return;
            for (int i = 1; i < stops.Count; i++)
            {
                Vector2 a = P(stops[i - 1].position), b = P(stops[i].position);
                int count = Mathf.Max(2, Mathf.CeilToInt(Vector2.Distance(a, b) / 15));
                for (int j = 0; j <= count; j++) NotebookStrokeGraphic.Dot(vh, Vector2.Lerp(a, b, (float)j / count), gold, 2);
            }
            foreach (var stop in stops) NotebookStrokeGraphic.Dot(vh, P(stop.position), gold, 7);
            if (owner.State.InvestigationOpen)
            {
                float progress = owner.State.JourneyProgress * (stops.Count - 1); JourneyConnection = Mathf.Min(stops.Count - 2, Mathf.FloorToInt(progress));
                JourneyMarker = Vector2.Lerp(stops[JourneyConnection].position, stops[JourneyConnection + 1].position, Mathf.Clamp(progress - JourneyConnection, .05f, .95f));
                NotebookStrokeGraphic.Dot(vh, P(JourneyMarker), new Color(.35f,.76f,.88f), 12);
            }
            else { JourneyConnection = stops.Count - 2; JourneyMarker = stops.Last().position; NotebookStrokeGraphic.Dot(vh, P(JourneyMarker), new Color(.35f,.76f,.88f), 12); }
        }
        void BuildTrain()
        {
            float width = page.rect.width, height = page.rect.height;
            var content = ScrollContent(page, new Vector2(8, -8), new Vector2(width - 16, 242), 242, true, 1290);
            var actors = FindObjectsByType<TrainActor>();
            for (int i = 0; i < 5; i++)
            {
                int index = i; string location = LocationIds[i];
                var card = Button(content, LocationNames[i], () => { selectedCar = LocationIds[index]; refresh = true; }, new Vector2(i * 258, 0), new Vector2(248, 228));
                card.GetComponentInChildren<Text>().alignment = TextAnchor.UpperCenter;
                var names = actors.Where(a => ClosestLocation(a.transform.position.x) == index).Select(a => a.role).ToList(); if (i == 0) names.Add("Madame Odette Vesper");
                Label(card, string.Join("\n", names), 17, paper, new Vector2(12, -39), new Vector2(224, 66));
                var collected = owner.State.progress.discoveries.Where(d => d.locationId == location).ToList();
                for (int j = 0; j < collected.Count; j++)
                {
                    var record = collected[j]; var clue = owner.catalogue.Clue(record.id); var statement = owner.catalogue.Statement(record.id);
                    var icon = Panel(card, record.id, clue != null ? gold : new Color(.4f,.7f,.8f)); Place(icon, new Vector2(12 + j % 8 * 28, -117 - j / 8 * 28), new Vector2(22, 22));
                    var hover = icon.gameObject.AddComponent<NotebookHover>(); hover.notebook = this; hover.text = (clue?.name ?? statement?.claim) + "\nFirst collected here / " + (record.day > 0 ? "day " + record.day : "day unknown");
                }
            }
            var records = owner.State.progress.discoveries.Where(d => d.locationId == selectedCar || (selectedCar == "unknown" && d.locationId == "unknown")).ToList();
            var list = ScrollContent(page, new Vector2(8, -264), new Vector2(width - 16, Mathf.Max(80, height - 274)), Mathf.Max(120, records.Count * 72 + 80));
            string place = Array.IndexOf(LocationIds, selectedCar) >= 0 ? LocationNames[Array.IndexOf(LocationIds, selectedCar)] : "Unknown location";
            Label(list, "Collected in " + place, 21, gold, new Vector2(12, -8), new Vector2(width - 40, 32));
            for (int i = 0; i < records.Count; i++) { var d = records[i]; Label(list, (d.day > 0 ? "Day " + d.day : "Day unknown") + " / " + (owner.catalogue.Clue(d.id)?.fact ?? owner.catalogue.Statement(d.id)?.claim), 18, paper, new Vector2(12, -48 - i * 72), new Vector2(width - 44, 68)); }
            if (owner.State.progress.discoveries.Any(d => d.locationId == "unknown")) Button(page, "Unknown provenance", () => { selectedCar = "unknown"; refresh = true; }, new Vector2(width - 205, -232), new Vector2(195, 30));
        }
        int ClosestLocation(float x) { int best = 0; float distance = float.PositiveInfinity; for (int i = 0; i < tour.roomViews.Length; i++) { float d = Mathf.Abs(x - tour.roomViews[i].position.x); if (d < distance) { distance = d; best = i; } } return best; }
        void BuildRemovedTheories()
        {
            float width = page.rect.width, height = page.rect.height;
            Label(page, "Removed theories — restore without replaying conversations", 20, gold, new Vector2(14, -8), new Vector2(width - 140, 55));
            Button(page, "Graph", () => { removedMode = false; refresh = true; }, new Vector2(width - 105, -8), new Vector2(90, 32));
            var content = ScrollContent(page, new Vector2(14, -72), new Vector2(width - 28, height - 85), Mathf.Max(80, owner.State.progress.removedTheories.Count * 76));
            for (int i = 0; i < owner.State.progress.removedTheories.Count; i++)
            {
                string id = owner.State.progress.removedTheories[i];
                Button(content, "Restore: " + owner.catalogue.Theory(id).text, () => { owner.State.RestoreTheory(id); refresh = true; }, new Vector2(0, -i * 76), new Vector2(width - 44, 70));
            }
        }
        void OnDestroy()
        {
            if (owner?.State != null) owner.State.Changed -= ScheduleRefresh;
            if (canvas != null) Destroy(canvas.gameObject); if (eventSystemCreated != null) Destroy(eventSystemCreated);
        }
    }
}
