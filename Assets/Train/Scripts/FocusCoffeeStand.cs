using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    [DefaultExecutionOrder(300), RequireComponent(typeof(TrainObservation), typeof(NarrativeInteraction))]
    public sealed class FocusCoffeeStand : MonoBehaviour
    {
        TrainNarrative owner;
        TrainTour tour;
        TrainObservation observation;
        GameObject markerRoot;
        RectTransform marker, tether;
        Button button;
        Text label;
        ClueMarker[] clueMarkers;
        readonly List<Material> materials = new List<Material>();
        public bool MarkerVisible => markerRoot != null && markerRoot.activeSelf;
        public static FocusCoffeeStand Ensure(TrainNarrative narrative, TrainTour train)
        {
            var existing = FindAnyObjectByType<FocusCoffeeStand>(); if (existing != null) { existing.GetComponent<NarrativeInteraction>().coffeeFocus = true; return existing; }
            var root = new GameObject("Coffee stand / focus break"); root.transform.SetParent(narrative.transform, false);
            float barX = train.roomViews.Length > 2 ? train.roomViews[2].position.x : 0;
            root.transform.position = new Vector3(barX + 2.5f, 1.7f, -1.8f);
            var stand = root.AddComponent<FocusCoffeeStand>();
            root.GetComponent<NarrativeInteraction>().coffeeFocus = true;
            var observation = root.GetComponent<TrainObservation>(); observation.title = "Coffee stand / restore focus"; observation.interactionRange = 2.8f;
            observation.description = "A coffee break will give you +" + narrative.State.CoffeeFocusReward + " focus for your doubt defenses.";
            stand.BuildModel(); return stand;
        }
        Material Surface(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); material.color = color;
            materials.Add(material); return material;
        }
        void Prop(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var part = GameObject.CreatePrimitive(type); part.name = name; part.transform.SetParent(transform, false);
            part.transform.localPosition = position; part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material; part.GetComponent<Collider>().enabled = solid;
        }
        void BuildModel()
        {
            var wood = Surface(new Color(.23f, .10f, .055f)); var brass = Surface(new Color(.67f, .43f, .17f));
            var porcelain = Surface(new Color(.93f, .89f, .77f)); var coffee = Surface(new Color(.09f, .035f, .015f));
            Prop("Coffee booth cabinet", PrimitiveType.Cube, new Vector3(0, -.45f, 0), new Vector3(1.35f, .8f, .7f), wood, true);
            Prop("Brass countertop", PrimitiveType.Cube, new Vector3(0, -.02f, 0), new Vector3(1.45f, .08f, .8f), brass);
            Prop("Coffee urn", PrimitiveType.Cylinder, new Vector3(.18f, .3f, .12f), new Vector3(.34f, .29f, .34f), brass);
            Prop("Urn lid", PrimitiveType.Sphere, new Vector3(.18f, .61f, .12f), new Vector3(.35f, .09f, .35f), brass);
            Prop("Urn tap", PrimitiveType.Cube, new Vector3(.18f, .19f, -.12f), new Vector3(.06f, .06f, .24f), brass);
            for (int i = 0; i < 2; i++)
            {
                var cup = new Vector3(-.45f + i * .24f, .12f, -.14f);
                Prop("Coffee cup " + i, PrimitiveType.Cylinder, cup, new Vector3(.14f, .09f, .14f), porcelain);
                Prop("Coffee surface " + i, PrimitiveType.Cylinder, cup + Vector3.up * .091f, new Vector3(.115f, .004f, .115f), coffee);
            }
        }
        void Start()
        {
            owner = FindAnyObjectByType<TrainNarrative>(); tour = owner == null ? null : owner.GetComponent<TrainTour>(); observation = GetComponent<TrainObservation>();
            if (owner == null || owner.State == null || tour == null) { enabled = false; return; }
            clueMarkers = FindObjectsByType<ClueMarker>();
            markerRoot = new GameObject("Coffee focus marker", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)); markerRoot.transform.SetParent(transform, false);
            var canvas = markerRoot.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 21;
            var line = new GameObject("Coffee stand tether", typeof(RectTransform), typeof(Image)); line.transform.SetParent(markerRoot.transform, false);
            tether = line.GetComponent<RectTransform>(); tether.pivot = new Vector2(0, .5f); line.GetComponent<Image>().color = new Color(.85f, .65f, .32f, .8f); line.GetComponent<Image>().raycastTarget = false;
            var go = new GameObject("Take coffee", typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(markerRoot.transform, false);
            marker = go.GetComponent<RectTransform>(); marker.sizeDelta = new Vector2(188, 52); go.GetComponent<Image>().color = new Color(.2f, .15f, .08f, .97f);
            button = go.GetComponent<Button>(); button.onClick.AddListener(() => tour.OpenObservation(observation));
            var text = new GameObject("Coffee reward", typeof(RectTransform), typeof(Text)); text.transform.SetParent(marker, false);
            label = text.GetComponent<Text>(); label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize = 17;
            label.color = new Color(1, .88f, .58f); label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one; label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        }
        void LateUpdate()
        {
            if (markerRoot == null) return;
            var camera = tour.viewCamera; var point = camera == null ? Vector3.back : camera.WorldToScreenPoint(transform.position + Vector3.up * .4f);
            bool visible = owner.State.Has("flag:investigation") && owner.State.CoffeeAvailable && !tour.exterior && !owner.Busy && point.z > 0 && point.x > 0 && point.x < Screen.width && point.y > 0 && point.y < Screen.height;
            markerRoot.SetActive(visible); if (!visible) return;
            Vector2 position = new Vector2(Mathf.Clamp(point.x, 100, Mathf.Max(100, Screen.width - 100)), Mathf.Clamp(point.y + 66, 75, Mathf.Max(75, Screen.height - 35)));
            foreach (float offset in new[] { 0f, 84f, -84f, 168f, -168f })
            {
                var candidate = new Vector2(position.x, Mathf.Clamp(position.y + offset, 75, Mathf.Max(75, Screen.height - 35)));
                var bounds = new Rect(candidate - new Vector2(94, 26), new Vector2(188, 52)); bool overlaps = false;
                foreach (var clue in clueMarkers) if (clue != null && clue.Visible && clue.ScreenBounds.Overlaps(bounds)) { overlaps = true; break; }
                if (!overlaps) { position = candidate; break; }
            }
            marker.position = position;
            var origin = new Vector2(point.x, point.y); var delta = position - Vector2.up * 26 - origin;
            tether.position = origin; tether.sizeDelta = new Vector2(delta.magnitude, 2); tether.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            button.interactable = tour.CanReach(observation);
            label.text = "COFFEE  +" + owner.State.CoffeeFocusReward + " FOCUS\n" + (button.interactable ? "E / Take a break" : "Approach the stand");
        }
        void OnDestroy()
        {
            if (markerRoot != null) Destroy(markerRoot);
            foreach (var material in materials) if (material != null) Destroy(material);
        }
    }
}
