using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GraduationDemo.Train
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DoubtBoardGraphic : MaskableGraphic, IPointerClickHandler, IPointerMoveHandler, IPointerExitHandler
    {
        public DoubtDefenseView view;
        Vector2? hover;
        DoubtDefense preview;
        Vector2 Point(Vector2 cell) => new Vector2(rectTransform.rect.xMin + (cell.x + .5f) * rectTransform.rect.width / DoubtDefense.Width,
            rectTransform.rect.yMax - (cell.y + .5f) * rectTransform.rect.height / DoubtDefense.Height) + (view.Feedback == null ? Vector2.zero : view.Feedback.Shake);
        void Update() { if (view != null && view.IsOpen) SetVerticesDirty(); }
        void Line(VertexHelper vh, Vector2 a, Vector2 b, Color color, float width = 2)
        {
            Vector2 side = (b - a).normalized; side = new Vector2(-side.y, side.x) * width * .5f; int start = vh.currentVertCount;
            vh.AddVert(a + side, color, Vector2.zero); vh.AddVert(b + side, color, Vector2.zero);
            vh.AddVert(b - side, color, Vector2.zero); vh.AddVert(a - side, color, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
        }
        void Dotted(VertexHelper vh, Vector2 a, Vector2 b, Color color)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b) / 12));
            for (int i = 0; i < count; i++) Line(vh, Vector2.Lerp(a, b, (float)i / count), Vector2.Lerp(a, b, (i + .5f) / count), color, 1.5f);
        }
        void Ring(VertexHelper vh, Vector2 center, float radiusX, float radiusY, Color color, float width = 2)
        {
            Vector2 previous = center + new Vector2(radiusX, 0);
            for (int i = 1; i <= 48; i++)
            { float angle = i * Mathf.PI * 2 / 48; var next = center + new Vector2(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY); Line(vh, previous, next, color, width); previous = next; }
        }
        void Area(VertexHelper vh, Vector2 center, float rx, float ry, Color color)
        {
            int start = vh.currentVertCount; vh.AddVert(center, color, Vector2.zero);
            for (int i = 0; i <= 32; i++) { float a = i * Mathf.PI / 16; vh.AddVert(center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry), color, Vector2.zero); }
            for (int i = 1; i <= 32; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }
        static Color KindColor(FocusTowerKind kind) => kind == FocusTowerKind.Projectile ? new Color(1, .8f, .4f) : kind == FocusTowerKind.Slow ? new Color(.3f, .85f, .8f) : new Color(.75f, .5f, 1);
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); if (view == null || view.Defense == null) return;
            var board = view.Defense; var rect = rectTransform.rect;
            float clock = view.Feedback != null && view.Feedback.ReducedMotion ? 0 : Time.unscaledTime;
            var topology = preview ?? board;
            foreach (var zone in topology.Draft.environment)
            {
                var color = zone.kind == DoubtZoneKind.Obstruction ? new Color(.45f, .49f, .5f, .85f) : zone.kind == DoubtZoneKind.InvaderBuff ? new Color(.95f, .25f, .35f, .2f) : new Color(1, .65f, .18f, .2f);
                float rx = zone.radius * rect.width / DoubtDefense.Width, ry = zone.radius * rect.height / DoubtDefense.Height;
                Area(vh, Point(zone.position), rx, ry, color); Ring(vh, Point(zone.position), rx, ry, new Color(color.r, color.g, color.b, .75f), 2);
                if (zone.kind != DoubtZoneKind.Obstruction)
                {
                    float pulse = .72f + Mathf.Repeat(clock * .35f, .28f);
                    Ring(vh, Point(zone.position), rx * pulse, ry * pulse, new Color(color.r, color.g, color.b, .3f));
                    // Distinct markings supplement the colors: cross = weakness, arrows = haste.
                    var p = Point(zone.position);
                    if (zone.kind == DoubtZoneKind.TowerDebuff) { Line(vh, p - new Vector2(5, 5), p + new Vector2(5, 5), color * 3); Line(vh, p + new Vector2(-5, 5), p + new Vector2(5, -5), color * 3); }
                    else { Line(vh, p + new Vector2(-6, -5), p + Vector2.up * 4, Color.white); Line(vh, p + new Vector2(6, -5), p + Vector2.up * 4, Color.white); }
                }
            }
            foreach (var entrance in DoubtDefense.Entrances)
            {
                if (board.Planning && view.SelectedEntry != null && entrance == DoubtDefense.Entrances[0])
                {
                    var path = topology.Path(entrance);
                    for (int i = 1; i < path.Count; i++) Dotted(vh, Point(path[i - 1]), Point(path[i]), new Color(.7f, .8f, .8f, .35f));
                }
                Ring(vh, Point(entrance), 11, 11, Color.white);
                var direction = (Point(DoubtDefense.Center) - Point(entrance)).normalized; var side = new Vector2(-direction.y, direction.x);
                var tip = Point(entrance) + direction * 6;
                Line(vh, Point(entrance) - direction * 6, tip, Color.white);
                Line(vh, tip, Point(entrance) + side * 4, Color.white); Line(vh, tip, Point(entrance) - side * 4, Color.white);
            }
            foreach (var wall in topology.Walls)
                if (wall.Accepted) NeuralInk.Axon(vh, Point(wall.a), Point(wall.b), NeuralInk.Violet, clock, 2);
                else if (preview != null) Dotted(vh, Point(wall.a), Point(wall.b), new Color(.95f, .4f, .45f));
            var center = Point(DoubtDefense.Center);
            float heartbeat = 1 + Mathf.Sin(clock * 2.5f) * .045f;
            var shield = board.Draft.defended ? new Color(.4f, 1, .75f) : new Color(.65f, .85f, .9f);
            Area(vh, center, 25 * heartbeat, 25 * heartbeat, new Color(shield.r, shield.g, shield.b, .08f));
            Ring(vh, center, 27 * heartbeat, 27 * heartbeat, shield, 1);
            NeuralInk.Neuron(vh, center, 24 * heartbeat, NeuralInk.Cyan, clock * .025f);
            Line(vh, center + Vector2.up * 18, center + Vector2.right * 18, Color.white, 3);
            Line(vh, center + Vector2.right * 18, center + Vector2.down * 18, Color.white, 3);
            Line(vh, center + Vector2.down * 18, center + Vector2.left * 18, Color.white, 3);
            Line(vh, center + Vector2.left * 18, center + Vector2.up * 18, Color.white, 3);
            foreach (var tower in topology.Draft.towers)
            {
                var kind = DoubtRules.Kind(board.State.catalogue, tower.entryId); var color = KindColor(kind);
                if (kind != FocusTowerKind.Projectile)
                {
                    float rx = topology.Range(tower) * rect.width / DoubtDefense.Width, ry = topology.Range(tower) * rect.height / DoubtDefense.Height;
                    Area(vh, Point(tower.Cell), rx, ry, new Color(color.r, color.g, color.b, .035f));
                    float pulse = .8f + Mathf.Repeat(clock * .2f + tower.x * .1f, .2f);
                    Ring(vh, Point(tower.Cell), rx * pulse, ry * pulse, new Color(color.r, color.g, color.b, .16f), 1);
                }
                Area(vh, Point(tower.Cell), 15, 15, new Color(color.r, color.g, color.b, .12f));
                Ring(vh, Point(tower.Cell), 13, 13, color, 4);
                NeuralInk.Neuron(vh, Point(tower.Cell), 15, color, tower.x + clock * .03f);
                // Category silhouettes remain readable independently of color and letter labels.
                if (kind == FocusTowerKind.Projectile) Line(vh, Point(tower.Cell) + Vector2.up * 12, Point(tower.Cell) + Vector2.up * 23, color, 5);
                if (kind == FocusTowerKind.Slow) { Line(vh, Point(tower.Cell) + Vector2.left * 19, Point(tower.Cell) + Vector2.left * 12, color, 3); Line(vh, Point(tower.Cell) + Vector2.right * 12, Point(tower.Cell) + Vector2.right * 19, color, 3); }
                if (kind == FocusTowerKind.Buff) Ring(vh, Point(tower.Cell), 19, 19, color, 1);
                if (tower.entryId == view.SelectedEntry)
                    Ring(vh, Point(tower.Cell), topology.Range(tower) * rect.width / DoubtDefense.Width, topology.Range(tower) * rect.height / DoubtDefense.Height, new Color(color.r, color.g, color.b, .45f), 1.5f);
                if (kind == FocusTowerKind.Buff && tower.entryId == view.SelectedEntry)
                    foreach (var target in topology.Draft.towers.Where(t => t != tower && Vector2.Distance(t.Cell, tower.Cell) <= topology.Range(tower)))
                        Dotted(vh, Point(tower.Cell), Point(target.Cell), color);
            }
            foreach (var invader in board.Draft.invaders.Where(i => i.health > 0 && !i.reachedCenter))
            {
                var position = view.Feedback == null ? invader.Cell : view.Feedback.Position(invader);
                var at = Point(position); float bob = 1 + Mathf.Sin(clock * 8 + invader.challengeIndex) * .08f;
                Area(vh, at, 14, 14, new Color(1, .2f, .3f, .10f));
                NeuralInk.Neuron(vh, at, 11, NeuralInk.Coral, clock * .4f + invader.challengeIndex);
                Ring(vh, at, 8 * bob, 10 / bob, new Color(1, .37f, .42f), 4);
                Line(vh, at + new Vector2(-3, 2), at + new Vector2(3, 2), Color.white, 2);
                var p = at + Vector2.up * 17;
                Line(vh, p - Vector2.right * 12, p - Vector2.right * 12 + Vector2.right * 24 * Mathf.Clamp01(invader.health / board.Tuning.invaderHealth), Color.red, 3);
            }
            if (view.Feedback != null) foreach (var burst in view.Feedback.Bursts)
            {
                float age = Mathf.Clamp01((Time.unscaledTime - burst.time) / burst.duration);
                var color = burst.color; color.a = 1 - age;
                var at = Point(burst.to);
                if (burst.projectile && view.Feedback.ReducedMotion)
                {
                    Line(vh, Point(burst.from), at, color, 2); Ring(vh, at, 12, 12, color); continue;
                }
                if (burst.projectile && age < .6f)
                {
                    var head = Vector2.Lerp(Point(burst.from), at, age / .6f);
                    NeuralInk.Axon(vh, Vector2.Lerp(Point(burst.from), at, Mathf.Max(0, age / .6f - .4f)), head, NeuralInk.Cyan, clock, 2);
                    Area(vh, head, 4, 4, Color.white);
                }
                else
                {
                    float progress = burst.projectile ? (age - .6f) / .4f : age;
                    float radius = view.Feedback.ReducedMotion ? 17 : 8 + progress * 38;
                    Ring(vh, at, radius, radius, color, 2 * (1 - progress) + 1);
                    if (!view.Feedback.ReducedMotion) for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4 + burst.to.x;
                        var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Line(vh, at + direction * radius * .8f, at + direction * (radius + 5), color, 2);
                    }
                }
            }
        }
        Vector2 Position(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, data.position, data.pressEventCamera, out var local);
            return new Vector2((local.x - rectTransform.rect.xMin) / rectTransform.rect.width * DoubtDefense.Width - .5f,
                (rectTransform.rect.yMax - local.y) / rectTransform.rect.height * DoubtDefense.Height - .5f);
        }
        public void OnPointerClick(PointerEventData data) => view.ClickPosition(Position(data));
        public void OnPointerMove(PointerEventData data)
        {
            var cell = Position(data); if (hover.HasValue && (hover.Value - cell).sqrMagnitude < .00001f) return; hover = cell; preview = null;
            if (view.Defense.Planning && view.SelectedEntry != null)
            {
                var draft = new DoubtDraft { doubtId = view.Defense.Definition.id, towers = view.Defense.Draft.towers.Select(t => new FocusPlacement { entryId = t.entryId, x = t.x, y = t.y }).ToList() };
                var candidate = new DoubtDefense(view.Defense.State, view.Defense.Definition, draft);
                if (candidate.Place(view.SelectedEntry, cell)) preview = candidate;
                view.SetPreview(candidate.Message + "\n" + string.Join(" ", candidate.Walls.Where(w => !w.Accepted).Select(w => w.rejection).Distinct()));
            }
            SetVerticesDirty();
        }
        public void OnPointerExit(PointerEventData data) { hover = null; preview = null; SetVerticesDirty(); }
    }
}
