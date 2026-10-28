using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    [DefaultExecutionOrder(200)]
    public sealed class SuspectQuestionMenu : MonoBehaviour
    {
        public bool IsOpen => root != null && root.activeSelf;
        public string Suspect { get; private set; }
        public IReadOnlyList<ConversationInitialization> Options { get; private set; } = new List<ConversationInitialization>();
        TrainNarrative owner;
        GameObject root;
        RectTransform window, content, viewport;
        Canvas canvas;
        Font font;
        bool refresh;
        Vector2 screenSize;
        readonly Color ink = new Color(.055f, .10f, .105f, .99f), gold = new Color(.95f, .85f, .62f);
        void Start()
        {
            owner = GetComponent<TrainNarrative>(); font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            root = new GameObject("Suspect questioning menu", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false); canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 120;
            window = Panel(root.transform, "Questioning panel", ink);
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(.5f, .5f);
            owner.State.Changed += ScheduleRefresh; root.SetActive(false);
        }
        void ScheduleRefresh() { refresh = true; }
        public bool Open(string suspect)
        {
            if (root == null || !owner.State.InvestigationOpen || !owner.CanTalkTo(suspect)) return false;
            Suspect = suspect; root.SetActive(true); refresh = true; Rebuild(); return true;
        }
        public void Close() { if (root != null) root.SetActive(false); }
        public bool Choose(string key)
        {
            if (!IsOpen || !owner.CanTalkTo(Suspect)) return false;
            var current = owner.State.AvailableConversations(Suspect).Find(p => p.key == key);
            if (current == null || current.blockedByDoubt || current.cost > owner.State.RemainingActions) { refresh = true; return false; }
            bool started = owner.OfferConversation(current); refresh = true; return started;
        }
        void LateUpdate()
        {
            if (!IsOpen) return;
            if (!owner.State.InvestigationOpen || !owner.CanTalkTo(Suspect)) { Close(); return; }
            if (screenSize != new Vector2(Screen.width, Screen.height)) refresh = true;
            if (refresh) Rebuild();
        }
        void Rebuild()
        {
            refresh = false; screenSize = new Vector2(Screen.width, Screen.height);
            canvas.scaleFactor = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), .45f, 1.25f);
            float width = Mathf.Min(1040, Screen.width / canvas.scaleFactor - 32), height = Screen.height / canvas.scaleFactor - 56;
            window.sizeDelta = new Vector2(width, height);
            foreach (Transform child in window) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            Label(window, "QUESTION " + Suspect.ToUpperInvariant(), new Vector2(24, -18), new Vector2(width - 160, 38), 26, gold);
            var close = Panel(window, "Close questioning", new Color(.18f, .27f, .28f)); Place(close, new Vector2(width - 120, -18), new Vector2(96, 34));
            var closeButton = close.gameObject.AddComponent<Button>(); closeButton.targetGraphic = close.GetComponent<Image>(); closeButton.onClick.AddListener(Close); Label(close, "Close / Esc", Vector2.zero, close.sizeDelta, 16, gold);
            Label(window, "Choose an unasked alibi or a question from your logbook. Diamonds and numbers match the reasoning graph.", new Vector2(24, -64), new Vector2(width - 48, 44), 18, gold);
            Label(window, owner.State.ActionCostsEnabled ? "Today: " + owner.State.RemainingActions + "/" + owner.State.ActionsPerDay + " actions" : "Action costs OFF — conversations are free", new Vector2(24, -108), new Vector2(width - 48, 30), 16, gold);
            viewport = Panel(window, "Available questions viewport", new Color(.07f, .14f, .15f));
            Place(viewport, new Vector2(20, -146), new Vector2(width - 40, Mathf.Max(90, height - 212))); viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.viewport = viewport;
            content = Panel(viewport, "Unasked conversations", Color.clear); content.GetComponent<Image>().raycastTarget = false;
            Options = owner.State.AvailableConversations(Suspect);
            Place(content, Vector2.zero, new Vector2(width - 40, Mathf.Max(viewport.rect.height, Options.Count * 148 + 8))); scroll.content = content;
            for (int i = 0; i < Options.Count; i++) AddOption(Options[i], i, width - 56);
            if (Options.Count == 0) Label(content, "No unasked conversations remain. Explore for clues and form new questions in the logbook.", new Vector2(18, -18), new Vector2(width - 86, 90), 20, gold);
            Label(window, owner.Message ?? "Selecting a paid conversation opens its action-cost confirmation.", new Vector2(24, -(height - 52)), new Vector2(width - 48, 44), 16, gold);
        }
        void AddOption(ConversationInitialization option, int index, float width)
        {
            var card = Panel(content, option.key, new Color(.12f, .22f, .23f)); Place(card, new Vector2(8, -8 - index * 148), new Vector2(width, 138));
            var button = card.gameObject.AddComponent<Button>(); button.targetGraphic = card.GetComponent<Image>(); button.interactable = !option.blockedByDoubt && option.cost <= owner.State.RemainingActions;
            string key = option.key; button.onClick.AddListener(() => Choose(key));
            bool alibi = option.kind == ConversationKind.Alibi;
            var question = owner.catalogue.Theory(option.theoryId);
            string number = alibi ? "A" : ReasoningGraphLayout.QuestionLabel(owner.State.progress, option.theoryId).Replace("Question ", "");
            Icon(card, alibi ? ReasoningNodeKind.Statement : ReasoningNodeKind.Theory, number, new Vector2(52, -53), 54, gold);
            if (!alibi)
            {
                Icon(card, ReasoningNodeKind.Clue, question.clueId, new Vector2(25, -108), 24, gold);
                Icon(card, ReasoningNodeKind.Statement, owner.catalogue.Statement(question.statementId).speaker.Substring(0, 1), new Vector2(78, -108), 24, new Color(.44f, .74f, .77f));
                Label(card, "+", new Vector2(41, -96), new Vector2(24, 24), 15, gold);
            }
            string label = alibi ? "Ask for an alibi" : ReasoningGraphLayout.QuestionLabel(owner.State.progress, option.theoryId);
            bool resolved = !alibi && owner.State.QuestionDoubtResolved(option.theoryId);
            Label(card, label + " · " + (option.blockedByDoubt ? "Needs doubt defense in Reasoning" : (resolved ? "Doubt resolved · " : "") + (option.cost == 0 ? "Free" : option.cost + " action(s)")), new Vector2(108, -10), new Vector2(width - 122, 26), 19, resolved ? new Color(.5f, .9f, .65f) : gold);
            Label(card, alibi ? "Where were you during the evening?" : question.text, new Vector2(108, -40), new Vector2(width - 128, 58), 18, Color.white);
            if (!alibi) Label(card, question.clueId + " / " + owner.catalogue.Clue(question.clueId).name + " + " + question.statementId + " / " + owner.catalogue.Statement(question.statementId).speaker,
                new Vector2(108, -102), new Vector2(width - 128, 32), 14, new Color(.71f, .77f, .73f));
        }
        void Icon(Transform parent, ReasoningNodeKind kind, string text, Vector2 position, float size, Color color)
        {
            var icon = new GameObject("Reasoning icon / " + text, typeof(RectTransform), typeof(NotebookNodeGraphic)); icon.transform.SetParent(parent, false);
            var rect = icon.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(size, size);
            var graphic = icon.GetComponent<NotebookNodeGraphic>(); graphic.kind = kind; graphic.color = color; graphic.raycastTarget = false;
            var label = Label(rect, text, Vector2.zero, new Vector2(size, size), size < 30 ? 10 : 20, ink); label.alignment = TextAnchor.MiddleCenter;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
        static RectTransform Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image)); go.transform.SetParent(parent, false); go.GetComponent<Image>().color = color; return go.GetComponent<RectTransform>();
        }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>(); label.text = text; label.font = font; label.fontSize = fontSize; label.color = color; label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate; Place(label.rectTransform, position, size); return label;
        }
        void OnDestroy() { if (owner != null && owner.State != null) owner.State.Changed -= ScheduleRefresh; if (root != null) Destroy(root); }
    }
}
