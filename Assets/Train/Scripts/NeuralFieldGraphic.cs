using UnityEngine;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    // Shared visual language for the detective's inner space. No navigation or combat state.
    public static class NeuralInk
    {
        public static readonly Color Cyan = new Color(.3f, .95f, 1);
        public static readonly Color Violet = new Color(.68f, .46f, 1);
        public static readonly Color Coral = new Color(1, .3f, .53f);
        public static void Line(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width = 2)
        {
            var normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f; int n = vh.currentVertCount;
            vh.AddVert(a + normal, color, Vector2.zero); vh.AddVert(b + normal, color, Vector2.zero);
            vh.AddVert(b - normal, color, Vector2.zero); vh.AddVert(a - normal, color, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2); vh.AddTriangle(n, n + 2, n + 3);
        }
        public static void Disc(VertexHelper vh, Vector2 at, float radius, Color color)
        {
            int n = vh.currentVertCount; vh.AddVert(at, color, Vector2.zero);
            for (int i = 0; i <= 20; i++) { float a = i * Mathf.PI / 10; vh.AddVert(at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero); }
            for (int i = 1; i <= 20; i++) vh.AddTriangle(n, n + i, n + i + 1);
        }
        public static void Axon(VertexHelper vh, Vector2 a, Vector2 b, Color color, float time, float width = 1.5f)
        {
            var direction = b - a; var normal = new Vector2(-direction.y, direction.x).normalized;
            // Keep even the full case graph within uGUI's 16-bit mesh vertex limit.
            int segments = Mathf.Clamp(Mathf.CeilToInt(direction.magnitude / 16), 3, 12);
            var previous = a;
            for (int i = 1; i <= segments; i++)
            {
                float p = (float)i / segments;
                float jitter = i == segments ? 0 : Mathf.Sin(i * 12.73f + Mathf.Floor(time * 12) * 2.1f + a.x) * Mathf.Sin(p * Mathf.PI) * 4;
                var next = Vector2.Lerp(a, b, p) + normal * jitter;
                var glow = color; glow.a *= .12f; Line(vh, previous, next, glow, width + 7); Line(vh, previous, next, color, width);
                previous = next;
            }
            float pulse = Mathf.Repeat(time * .27f + a.y * .003f, 1);
            var at = Vector2.Lerp(a, b, pulse); var halo = color; halo.a *= .2f;
            Disc(vh, at, 7, halo); Disc(vh, at, 2.3f, color);
        }
        public static void Neuron(VertexHelper vh, Vector2 at, float radius, Color color, float phase)
        {
            var halo = color; halo.a *= .12f; Disc(vh, at, radius * 1.7f, halo); Disc(vh, at, radius * .38f, color);
            for (int i = 0; i < 6; i++)
            {
                float angle = phase + i * Mathf.PI / 3;
                var d = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)); var side = new Vector2(-d.y, d.x);
                var elbow = at + d * radius * .8f;
                Line(vh, at + d * radius * .25f, elbow, color, 1.4f);
                Line(vh, elbow, at + d * radius * 1.3f + side * radius * .25f, color, 1);
                Line(vh, elbow, at + d * radius * 1.1f - side * radius * .35f, color, 1);
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NeuralFieldGraphic : MaskableGraphic
    {
        public bool reducedMotion;
        public static NeuralFieldGraphic Attach(RectTransform parent, bool reduced)
        {
            var go = new GameObject("Neural mind-space", typeof(RectTransform), typeof(NeuralFieldGraphic)); go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var field = go.GetComponent<NeuralFieldGraphic>(); field.raycastTarget = false; field.reducedMotion = reduced; return field;
        }
        void Update() { if (!reducedMotion) SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var rect = rectTransform.rect; float t = reducedMotion ? 0 : Time.unscaledTime;
            for (int i = 0; i < 19; i++)
            {
                float x = Mathf.Repeat(i * .618034f + .07f, 1), y = Mathf.Repeat(i * .414214f + .13f, 1);
                var at = new Vector2(rect.xMin + x * rect.width, rect.yMin + y * rect.height);
                at += new Vector2(Mathf.Sin(t * .13f + i), Mathf.Cos(t * .11f + i)) * 5;
                var c = i % 3 == 0 ? NeuralInk.Coral : NeuralInk.Violet; c.a = .06f;
                NeuralInk.Neuron(vh, at, 15 + i % 4 * 7, c, i * 1.3f);
                if (i % 2 == 0)
                {
                    var end = at + new Vector2(Mathf.Cos(i * 2.3f), Mathf.Sin(i * 2.3f)) * 150;
                    c.a = .03f; NeuralInk.Axon(vh, at, end, c, t, 1);
                }
            }
        }
    }
}
