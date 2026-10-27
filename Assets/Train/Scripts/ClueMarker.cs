using GraduationDemo.Narrative;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    [DefaultExecutionOrder(250), RequireComponent(typeof(NarrativeInteraction), typeof(TrainObservation))]
    public sealed class ClueMarker : MonoBehaviour
    {
        [Tooltip("World-space offset from this clue's inspection point.")]
        public Vector3 markerOffset = new Vector3(0, .2f, 0);
        public bool Revealed => owner != null && owner.State != null && owner.State.InvestigationOpen &&
            owner.State.Has(binding.requiredFlag) && owner.State.CanFindClue(binding.clueId) && !owner.State.Has(binding.clueId);
        public bool Interactable => Revealed && !owner.Busy && tour.CanReach(observation);
        public bool Visible => root != null && root.activeSelf;
        public Rect ScreenBounds { get; private set; }
        static readonly List<ClueMarker> active = new List<ClueMarker>();
        static int layoutFrame = -1;
        TrainNarrative owner;
        TrainTour tour;
        NarrativeInteraction binding;
        TrainObservation observation;
        GameObject root;
        RectTransform marker, tether;
        Button button;
        Text caption;
        void OnEnable() { if (!active.Contains(this)) active.Add(this); layoutFrame = -1; }

        void Start()
        {
            binding = GetComponent<NarrativeInteraction>(); observation = GetComponent<TrainObservation>();
            owner = FindAnyObjectByType<TrainNarrative>(); tour = owner == null ? null : owner.GetComponent<TrainTour>();
            if (owner == null || string.IsNullOrEmpty(binding.clueId)) { enabled = false; return; }
            root = new GameObject("World clue marker / " + binding.clueId, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            var line = new GameObject("Tether to inspection point", typeof(RectTransform), typeof(Image)); line.transform.SetParent(root.transform, false);
            tether = line.GetComponent<RectTransform>(); tether.pivot = new Vector2(0, .5f);
            line.GetComponent<Image>().color = new Color(.95f, .85f, .62f, .65f); line.GetComponent<Image>().raycastTarget = false;
            var node = new GameObject("Inspect clue at world position", typeof(RectTransform), typeof(NotebookNodeGraphic), typeof(Button));
            node.transform.SetParent(root.transform, false); marker = node.GetComponent<RectTransform>(); marker.sizeDelta = new Vector2(42, 42);
            var graphic = node.GetComponent<NotebookNodeGraphic>(); graphic.kind = ReasoningNodeKind.Clue; graphic.color = new Color(.95f, .85f, .62f);
            button = node.GetComponent<Button>(); button.targetGraphic = graphic; button.onClick.AddListener(() => tour.OpenObservation(observation));
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var symbol = new GameObject("Generic clue symbol", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            symbol.transform.SetParent(marker, false); symbol.font = font; symbol.text = "?"; symbol.fontSize = 25; symbol.alignment = TextAnchor.MiddleCenter;
            symbol.color = new Color(.07f, .14f, .15f); symbol.raycastTarget = false; symbol.rectTransform.sizeDelta = new Vector2(42, 42);
            caption = new GameObject("Approach and inspect", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            caption.transform.SetParent(marker, false); caption.font = font; caption.fontSize = 15; caption.alignment = TextAnchor.UpperCenter;
            caption.color = new Color(.96f, .91f, .77f); caption.raycastTarget = false; caption.rectTransform.sizeDelta = new Vector2(190, 46);
            caption.rectTransform.anchoredPosition = new Vector2(0, -48);
            var shadow = caption.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0, 0, 0, .9f); shadow.effectDistance = new Vector2(1, -1);
        }
        void LateUpdate()
        {
            if (layoutFrame == Time.frameCount) return;
            layoutFrame = Time.frameCount;
            var occupied = new List<Rect>();
            foreach (var item in active.Where(m => m != null && m.root != null).OrderBy(m => m.binding.clueId)) item.Layout(occupied);
        }
        void Layout(List<Rect> occupied)
        {
            Vector3 point = tour.viewCamera.WorldToScreenPoint(transform.position + markerOffset);
            float nearest = float.PositiveInfinity; int room = -1;
            for (int i = 0; i < tour.roomViews.Length; i++)
            {
                float distance = Mathf.Abs(transform.position.x - tour.roomViews[i].position.x);
                if (distance < nearest) { nearest = distance; room = i; }
            }
            bool sameRoom = room == tour.currentRoom;
            bool show = Revealed && !owner.Busy && !tour.exterior && sameRoom && point.z > 0 && point.x > 0 && point.x < Screen.width && point.y > 0 && point.y < Screen.height;
            root.SetActive(show); if (!show) return;
            Vector2 desired = new Vector2(point.x, point.y + 32), position = desired;
            float best = float.PositiveInfinity;
            void Consider(Vector2 candidate)
            {
                candidate.x = Mathf.Clamp(candidate.x, 76, Mathf.Max(76, Screen.width - 76));
                candidate.y = Mathf.Clamp(candidate.y, 66, Mathf.Max(66, Screen.height - 28));
                var bounds = BoundsAt(candidate); float score = (candidate - desired).sqrMagnitude;
                if (score < best && occupied.All(other => !other.Overlaps(bounds))) { best = score; position = candidate; }
            }
            for (int y = -3; y <= 3; y++) for (int x = -3; x <= 3; x++) Consider(desired + new Vector2(x * 154, y * 84));
            if (float.IsPositiveInfinity(best))
                for (float y = 66; y <= Screen.height - 28; y += 84) for (float x = 76; x <= Screen.width - 76; x += 154) Consider(new Vector2(x, y));
            ScreenBounds = BoundsAt(position); occupied.Add(ScreenBounds);
            marker.position = position;
            Vector2 start = new Vector2(point.x, point.y), delta = position - start;
            tether.position = start; tether.sizeDelta = new Vector2(delta.magnitude, 2);
            tether.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            button.interactable = Interactable;
            caption.text = Interactable ? "E / Inspect" : "Approach to inspect";
        }
        static Rect BoundsAt(Vector2 position) => new Rect(position.x - 75, position.y - 59, 150, 82);
        void OnDisable() { active.Remove(this); layoutFrame = -1; if (root != null) root.SetActive(false); }
        void OnDestroy() { if (root != null) Destroy(root); }
    }
}
