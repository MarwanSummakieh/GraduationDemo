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
            if (owner.State == null) { enabled = false; return; }
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
        void ScheduleRefresh() { refresh = true; }
        public void SetOpen(bool open)
        {
            if (window == null) return;
            window.gameObject.SetActive(open); HideTooltip();
            if (open) { refresh = true; firstFit = true; }
        }
        public void SelectTab(NotebookTab tab) { Tab = tab; firstFit = true; refresh = true; }
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
            if (Tab == NotebookTab.Reasoning && Graph != null && !accusationMode)
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
            if (status != null) status.text = owner.Message ?? "Select a clue and statement to make a theory.";
        }
        void Rebuild()
        {
            accusation.RemoveAll(id => !owner.State.progress.theories.Contains(id));
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
            if (accusationMode && owner.State.progress.phase == InvestigationPhase.Accusation) { BuildAccusation(); return; }
            accusationMode = false;
            float width = page.rect.width, height = page.rect.height; bool narrow = width < 880;
            viewport = Panel(page, "Reasoning graph viewport", new Color(.065f, .12f, .13f));
            Place(viewport, new Vector2(8, -52), new Vector2(narrow ? width - 16 : width - 300, narrow ? height - 281 : height - 64)); viewport.gameObject.AddComponent<RectMask2D>();
            graphContent = new GameObject("Graph", typeof(RectTransform)).GetComponent<RectTransform>(); graphContent.SetParent(viewport, false); graphContent.anchorMin = graphContent.anchorMax = new Vector2(.5f, .5f); graphContent.pivot = new Vector2(0, 1);
            Graph = ReasoningGraphLayout.Build(owner.catalogue, owner.State.progress, search, suspectFilter); graphContent.sizeDelta = Graph.size;
            var edgeObject = new GameObject("Parent connections", typeof(RectTransform), typeof(NotebookStrokeGraphic)); edgeObject.transform.SetParent(graphContent, false);
            var edgeRect = edgeObject.GetComponent<RectTransform>(); Stretch(edgeRect, 0, 0, 0, 0); edgeRect.pivot = new Vector2(0, 1);
            strokes = edgeObject.GetComponent<NotebookStrokeGraphic>(); strokes.notebook = this; strokes.raycastTarget = false;
            navigation = viewport.gameObject.AddComponent<NotebookPanZoom>(); navigation.content = graphContent; navigation.notebook = this;
            foreach (var node in Graph.nodes) AddNode(node);
            // Layout groups remain constrained to clues / authored theories / suspect statements.
            Label(graphContent, "CLUES", 24, gold, new Vector2(40, -8), new Vector2(190, 35));
            Label(graphContent, "THEORIES", 24, gold, new Vector2(380, -8), new Vector2(260, 35));
            Label(graphContent, "STATEMENTS", 24, gold, new Vector2(920, -8), new Vector2(230, 35));
            foreach (var speaker in new[] { "Felix", "Mila", "Bellamy" })
            {
                var first = Graph.nodes.FirstOrDefault(n => n.kind == ReasoningNodeKind.Statement && n.speaker == speaker);
                if (first != null) Label(graphContent, speaker, 20, paper, new Vector2(1090, -first.position.y - 8), new Vector2(70, 55));
            }
            Canvas.ForceUpdateCanvases();
            if (preserveView) { navigation.zoom = savedZoom; graphContent.localScale = Vector3.one * savedZoom; graphContent.anchoredPosition = savedPan; }
            else
            {
                navigation.Fit(Graph.size);
                // Start with usable hit targets; Fit remains an explicit overview for a large graph.
                if (navigation.zoom < .45f)
                {
                    navigation.zoom = .45f; graphContent.localScale = Vector3.one * .45f;
                    graphContent.anchoredPosition = new Vector2(-Graph.size.x * .45f * .5f, viewport.rect.height * .5f - 35);
                }
            }
            firstFit = false;
            AddSearch();
            Button(page, string.IsNullOrEmpty(suspectFilter) ? "All" : suspectFilter, () => { suspectFilter = suspectFilter == "" ? "Felix" : suspectFilter == "Felix" ? "Mila" : suspectFilter == "Mila" ? "Bellamy" : ""; firstFit = true; refresh = true; }, new Vector2(248, -8), new Vector2(90, 32));
            Button(page, "Fit", () => navigation.Fit(Graph.size), new Vector2(346, -8), new Vector2(70, 32));
            Button(page, "Removed: " + owner.State.progress.removedTheories.Count, () => { removedMode = true; refresh = true; }, new Vector2(424, -8), new Vector2(104, 32));
            sidebar = Panel(page, "Theory detail", ink);
            Place(sidebar, new Vector2(narrow ? 8 : width - 280, narrow ? -(height - 217) : -52), new Vector2(narrow ? width - 16 : 272, narrow ? 207 : height - 64));
            float detailHeight = narrow ? 66 : sidebar.rect.height - 208;
            var detailContent = ScrollContent(sidebar, new Vector2(10, -8), new Vector2(sidebar.rect.width - 20, detailHeight), 400);
            detail = Label(detailContent, SelectedDetail(), 18, gold, Vector2.zero, new Vector2(sidebar.rect.width - 28, 400));
            float textHeight = Mathf.Max(detailHeight, detail.preferredHeight + 16); detailContent.sizeDelta = new Vector2(detailContent.sizeDelta.x, textHeight); detail.rectTransform.sizeDelta = new Vector2(detail.rectTransform.sizeDelta.x, textHeight);
            if (owner.State.progress.phase == InvestigationPhase.Accusation)
                Button(sidebar, "Build final accusation", () => { accusationMode = true; refresh = true; }, new Vector2(10, narrow ? -115 : -(sidebar.rect.height - 108)), new Vector2(sidebar.rect.width - 20, 32));
            else if (owner.State.InvestigationOpen)
                for (int i = 0; i < 3; i++)
                {
                    string target = new[] { "Felix", "Mila", "Bellamy" }[i];
                    var rect = Button(sidebar, "Present to " + target, () => owner.Confront(selectedTheory, target), new Vector2(narrow ? 10 + i * (sidebar.rect.width - 20) / 3 : 10, narrow ? -115 : -(sidebar.rect.height - 144 + i * 36)), new Vector2(narrow ? (sidebar.rect.width - 26) / 3 : sidebar.rect.width - 20, 32));
                    rect.GetComponent<Button>().interactable = selectedTheory != null && owner.State.Presentation(selectedTheory, target) != null;
                }
            status = Label(sidebar, owner.Message ?? "Click a clue, then a statement. Drag to pan; scroll to zoom.", 16, paper, new Vector2(10, -(sidebar.rect.height - 52)), new Vector2(sidebar.rect.width - 20, 48));
            if (selectedTheory != null)
                Button(sidebar, "Remove theory", () => RemoveSelectedTheory(), new Vector2(10, narrow ? -79 : -(sidebar.rect.height - 180)), new Vector2(sidebar.rect.width - 20, 28));
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
            rect.anchoredPosition = positions.TryGetValue(node.id, out var prior) ? prior : new Vector2(node.position.x, -node.position.y);
            nodeRects.Add(node.id, rect); positions[node.id] = rect.anchoredPosition;
            var graphic = go.GetComponent<NotebookNodeGraphic>(); graphic.kind = node.kind;
            graphic.color = node.kind == ReasoningNodeKind.Clue ? gold : node.speaker == "Mila" ? new Color(.38f, .67f, .84f) : node.speaker == "Felix" ? new Color(.84f, .64f, .37f) : new Color(.45f, .74f, .64f);
            if (node.id == selectedClue || node.id == selectedStatement || node.id == selectedTheory) graphic.color = Color.Lerp(graphic.color, Color.white, .45f);
            go.GetComponent<Button>().targetGraphic = graphic; go.GetComponent<Button>().onClick.AddListener(() => SelectNode(node.id));
            var hover = go.GetComponent<NotebookHover>(); hover.notebook = this; hover.text = node.label + "\n" + node.detail;
            string icon = node.kind == ReasoningNodeKind.Theory ? node.label.Replace("Theory ", "") : node.kind == ReasoningNodeKind.Clue ? node.id : node.speaker.Substring(0, 1) + node.id.Substring(node.id.Length - 2);
            var inside = Label(rect, icon, 18, ink, Vector2.zero, new Vector2(60, 60)); Stretch(inside.rectTransform, 0, 0, 0, 0); inside.alignment = TextAnchor.MiddleCenter;
            var caption = Label(rect, node.label, 17, paper, new Vector2(-60, -62), new Vector2(180, 50)); caption.alignment = TextAnchor.UpperCenter;
            caption.raycastTarget = true;
            if (node.kind == ReasoningNodeKind.Theory) { caption.rectTransform.sizeDelta = new Vector2(76, 45); caption.rectTransform.anchoredPosition = new Vector2(-8, -62); }
        }
        public void SelectNode(string id)
        {
            var node = Graph?.nodes.Find(n => n.id == id); if (node == null) return;
            if (node.kind == ReasoningNodeKind.Clue) selectedClue = id;
            else if (node.kind == ReasoningNodeKind.Statement) selectedStatement = id;
            else { selectedTheory = id; selectedClue = null; selectedStatement = null; }
            if (node.kind != ReasoningNodeKind.Theory && selectedClue != null && selectedStatement != null && owner.State.InvestigationOpen)
            {
                var theory = owner.State.Construct(selectedClue, selectedStatement);
                if (theory != null) { selectedTheory = theory.id; selectedClue = null; selectedStatement = null; }
            }
            refresh = true;
        }
        public bool RemoveSelectedTheory()
        {
            if (selectedTheory == null) return false;
            string id = selectedTheory;
            if (!owner.State.RemoveTheory(id)) return false;
            accusation.Remove(id); positions.Remove(id); selectedTheory = null; selectedClue = null; selectedStatement = null;
            refresh = true; return true;
        }
        string SelectedDetail()
        {
            var theory = owner.catalogue.Theory(selectedTheory);
            if (theory != null) return theory.text + "\n\nClue: " + owner.catalogue.Clue(theory.clueId).name + "\nStatement: " + owner.catalogue.Statement(theory.statementId).claim;
            return "Click a clue and a statement to make a theory. Drag to pan; scroll to zoom. Hover over any icon for its details.";
        }
        public Vector2 NodePosition(string id) => positions.TryGetValue(id, out var value) ? value : Vector2.zero;
        public void ShowTooltip(string text, Vector2 screenPosition)
        {
            if (tooltip == null) return; tooltipText.text = text;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(window, screenPosition, null, out var local);
            tooltip.anchoredPosition = new Vector2(Mathf.Clamp(local.x + 16, window.rect.xMin + 8, window.rect.xMax - 390), Mathf.Clamp(local.y - 12, window.rect.yMin + 150, window.rect.yMax - 8));
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
        void BuildAccusation()
        {
            float width = page.rect.width, height = page.rect.height;
            Label(page, "Select three independent theories (" + accusation.Count + "/3)", 23, gold, new Vector2(14, -8), new Vector2(width - 150, 36));
            Button(page, "Graph", () => { accusationMode = false; refresh = true; }, new Vector2(width - 105, -8), new Vector2(90, 32));
            var content = ScrollContent(page, new Vector2(14, -54), new Vector2(width - 28, height - 173), owner.State.progress.theories.Count * 76);
            for (int i = 0; i < owner.State.progress.theories.Count; i++)
            {
                string id = owner.State.progress.theories[i];
                Button(content, (accusation.Contains(id) ? "✓ " : "") + owner.catalogue.Theory(id).text, () => { if (accusation.Contains(id)) accusation.Remove(id); else if (accusation.Count < 3) accusation.Add(id); refresh = true; }, new Vector2(0, -i * 76), new Vector2(width - 44, 70));
            }
            for (int i = 0; i < 3; i++) { string target = new[] { "Felix", "Mila", "Bellamy" }[i]; Button(page, "Accuse " + target, () => owner.Accuse(target, accusation), new Vector2(14 + i * (width - 28) / 3, -(height - 105)), new Vector2((width - 40) / 3, 34)); }
            status = Label(page, owner.Message ?? "Review the evidence before accusing a suspect.", 18, paper, new Vector2(14, -(height - 61)), new Vector2(width - 28, 55));
        }
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
