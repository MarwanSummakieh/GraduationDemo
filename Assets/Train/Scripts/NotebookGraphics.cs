using System.Collections.Generic;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GraduationDemo.Train
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NotebookStrokeGraphic : MaskableGraphic
    {
        public InvestigationNotebook notebook;
        public bool map;
        public static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width = 2)
        {
            if ((a - b).sqrMagnitude < .01f) return;
            Vector2 normal = new Vector2(-(b - a).y, (b - a).x).normalized * width * .5f;
            int i = vh.currentVertCount;
            vh.AddVert(a - normal, color, Vector2.zero); vh.AddVert(a + normal, color, Vector2.zero);
            vh.AddVert(b + normal, color, Vector2.zero); vh.AddVert(b - normal, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
        public static void Dot(VertexHelper vh, Vector2 position, Color color, float radius)
        {
            int i = vh.currentVertCount; vh.AddVert(position, color, Vector2.zero);
            for (int j = 0; j <= 20; j++) { float a = j * Mathf.PI * .1f; vh.AddVert(position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, color, Vector2.zero); }
            for (int j = 1; j <= 20; j++) vh.AddTriangle(i, i + j, i + j + 1);
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (notebook == null) return;
            if (map) { notebook.DrawMap(vh, rectTransform.rect); return; }
            var model = notebook.Graph; if (model == null) return;
            foreach (var node in model.nodes)
            {
                if (node.kind != ReasoningNodeKind.Theory) continue;
                Vector2 here = notebook.NodePosition(node.id);
                Stroke(vh, notebook.NodePosition(node.clueParent), here, new Color(.72f, .62f, .39f, .55f));
                Stroke(vh, here, notebook.NodePosition(node.statementParent), new Color(.42f, .72f, .72f, .55f));
            }
        }
    }
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class NotebookNodeGraphic : MaskableGraphic
    {
        public ReasoningNodeKind kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); Vector2 middle = rectTransform.rect.center; float r = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .43f;
            if (kind == ReasoningNodeKind.Statement) { NotebookStrokeGraphic.Dot(vh, middle, color, r); return; }
            Vector2[] corners = kind == ReasoningNodeKind.Theory ? new[] { middle + Vector2.up * r, middle + Vector2.right * r, middle + Vector2.down * r, middle + Vector2.left * r } :
                new[] { middle + new Vector2(-r, r), middle + new Vector2(r, r), middle + new Vector2(r, -r), middle + new Vector2(-r, -r) };
            for (int i = 0; i < 4; i++) vh.AddVert(corners[i], color, Vector2.zero);
            vh.AddTriangle(0, 1, 2); vh.AddTriangle(0, 2, 3);
        }
    }
    public sealed class NotebookHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public InvestigationNotebook notebook;
        public string text;
        public void OnPointerEnter(PointerEventData data) { notebook.ShowTooltip(text, data.position); }
        public void OnPointerExit(PointerEventData data) { notebook.HideTooltip(); }
    }
    public sealed class NotebookPanZoom : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        public RectTransform content;
        public InvestigationNotebook notebook;
        public float zoom = 1;
        public void OnBeginDrag(PointerEventData data) { notebook.HideTooltip(); }
        public void OnDrag(PointerEventData data) { content.anchoredPosition += data.delta / GetComponentInParent<Canvas>().scaleFactor; }
        public void OnScroll(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, null, out var pointer);
            Vector2 point = (pointer - content.anchoredPosition) / zoom;
            zoom = Mathf.Clamp(zoom * Mathf.Pow(1.16f, Mathf.Clamp(data.scrollDelta.y, -3, 3)), .035f, 2.2f);
            content.localScale = Vector3.one * zoom; content.anchoredPosition = pointer - point * zoom;
            notebook.HideTooltip();
        }
        public void Fit(Vector2 size)
        {
            var viewport = ((RectTransform)transform).rect.size;
            zoom = Mathf.Clamp(Mathf.Min(viewport.x / (size.x + 80), viewport.y / (size.y + 80)), .035f, 1);
            content.localScale = Vector3.one * zoom;
            content.anchoredPosition = new Vector2(-size.x * zoom * .5f, size.y * zoom * .5f);
        }
        public void Focus(Vector2 position) { content.anchoredPosition = new Vector2(-position.x, position.y) * zoom; }
    }
}
