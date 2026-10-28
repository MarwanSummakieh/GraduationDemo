using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    public sealed class FocusWall
    {
        public Vector2 a, b;
        public string rejection;
        public bool Accepted => rejection == null;
    }
    public sealed class FocusEffect
    {
        public FocusTowerKind kind;
        public Vector2 from, to;
    }
    // Continuous visibility routing around tower discs and wall segments. No spatial grid.
    public sealed class DoubtDefense
    {
        public const int Width = 17, Height = 13;
        public static readonly Vector2 Center = new Vector2(8, 6);
        public static readonly Vector2[] Entrances = { new Vector2(0, 6), new Vector2(16, 6), new Vector2(8, 0), new Vector2(8, 12) };
        List<Vector2> routePoints;
        float[] routeDistance;
        int[] routeNext;
        const float TowerClearance = .55f, WallClearance = .12f;
        public readonly InvestigationState State;
        public readonly DoubtDefinition Definition;
        public readonly DoubtDraft Draft;
        public DoubtTuning Tuning => State.configuration?.doubtTuning ?? new DoubtTuning();
        public readonly List<FocusWall> Walls = new List<FocusWall>();
        public readonly List<FocusEffect> Effects = new List<FocusEffect>();
        public string Message { get; private set; }
        public int FocusUsed => Draft.towers.Sum(t => DoubtRules.Cost(State.catalogue, t.entryId));
        public bool Planning => !Draft.started;
        public string Interpretation => string.IsNullOrWhiteSpace(Draft.revisedInterpretation) ? Definition.interpretation : Draft.revisedInterpretation;
        public bool RoutesOpen => Entrances.All(e => Path(e).Count > 0);
        public DoubtDefense(InvestigationState state, DoubtDefinition definition, DoubtDraft draft)
        {
            State = state; Definition = definition; Draft = draft;
            // Archived questions remain in notebook history, but cannot be deployed as active material.
            bool removed = Draft.towers.RemoveAll(t => !State.HasAccusationEntry(t.entryId)) > 0;
            if (Draft.environment == null) Draft.environment = new List<DoubtZone>();
            if (Draft.batchLinkIds == null) Draft.batchLinkIds = new List<string>();
            if (Draft.pressureCaptured)
                Definition.challenges = Draft.batchLinkIds.Select(id => DoubtRules.Links(State).FirstOrDefault(d => d.id == id)).Where(d => d != null)
                    .Select(d => State.catalogue.Theory(d.questionId).text + " / " + d.reason).ToList();
            if (Definition.challenges.Count == 0) Definition.challenges.Add(Definition.explanation);
            if (removed || Draft.started && Draft.navigationVersion < 2) Reset(); else { if (Planning) PreparePressure(); RebuildWalls(); }
        }
        public void RefreshPressure() { if (Planning && Draft.pressure != State.DoubtPressure) { PreparePressure(); RebuildWalls(); } }
        void PreparePressure()
        {
            Draft.pressure = State.DoubtPressure;
            int level = Mathf.Min(Tuning.maxDifficulty, Draft.pressure);
            Draft.waves = Mathf.Min(Tuning.maxWaves, 1 + Mathf.Max(0, level - 1) / 2);
            Draft.environment.Clear();
            var rocks = new[] { new Vector2(4.8f, 2.6f), new Vector2(11.2f, 9.4f), new Vector2(12, 3) };
            var haste = new[] { new Vector2(3.1f, 6), new Vector2(13, 6) };
            var weakness = new[] { new Vector2(5.5f, 8.5f), new Vector2(10.5f, 3.5f) };
            for (int i = 0; i < Mathf.Min(3, level / 2); i++) Draft.environment.Add(new DoubtZone { kind = DoubtZoneKind.Obstruction, position = rocks[i], radius = .65f });
            for (int i = 0; i < Mathf.Min(2, level / 3); i++) Draft.environment.Add(new DoubtZone { kind = DoubtZoneKind.InvaderBuff, position = haste[i], radius = 1.2f });
            for (int i = 0; i < Mathf.Min(2, level / 2); i++) Draft.environment.Add(new DoubtZone { kind = DoubtZoneKind.TowerDebuff, position = weakness[i], radius = 1.3f });
            routePoints = null;
        }
        public float TowerEnvironmentMultiplier(Vector2 position) => Draft.environment.Any(z => z.kind == DoubtZoneKind.TowerDebuff && Vector2.Distance(z.position, position) <= z.radius) ? Tuning.environmentalTowerMultiplier : 1;
        public float InvaderEnvironmentMultiplier(Vector2 position) => Draft.environment.Any(z => z.kind == DoubtZoneKind.InvaderBuff && Vector2.Distance(z.position, position) <= z.radius) ? Tuning.environmentalInvaderSpeed : 1;
        public bool Place(string id, Vector2 cell)
        {
            if (!Planning || !State.CanBuildAccusation || !State.HasAccusationEntry(id)) { Message = "Use a collected entry during planning."; return false; }
            RefreshPressure();
            if (Draft.environment.Any(z => z.kind == DoubtZoneKind.Obstruction && Vector2.Distance(z.position, cell) < z.radius + .35f)) { Message = "That position overlaps an obstruction."; return false; }
            if (float.IsNaN(cell.x) || float.IsNaN(cell.y) || cell.x < .35f || cell.y < .35f || cell.x > Width - 1.35f || cell.y > Height - 1.35f || Vector2.Distance(cell, Center) < 2)
            { Message = "Leave the entrances and the defended center clear."; return false; }
            var existing = Draft.towers.Find(t => t.entryId == id);
            if (Draft.towers.Any(t => t != existing && Vector2.Distance(t.Cell, cell) < .8f)) { Message = "Leave enough space between tower bases."; return false; }
            if (existing == null && FocusUsed + DoubtRules.Cost(State.catalogue, id) > State.FocusCapacity)
            { Message = "Focus is full. Remove or move an active tower; notebook questions are preserved."; return false; }
            var old = existing == null ? (Vector2?)null : existing.Cell;
            if (existing == null) { existing = new FocusPlacement { entryId = id }; Draft.towers.Add(existing); }
            existing.x = cell.x; existing.y = cell.y; RebuildWalls();
            if (!RoutesOpen)
            {
                if (old.HasValue) { existing.x = old.Value.x; existing.y = old.Value.y; } else Draft.towers.Remove(existing);
                RebuildWalls(); Message = "Placement would close an entrance's route to the center."; return false;
            }
            Message = "Placed " + id + ". " + Walls.Count(w => !w.Accepted) + " wall connection(s) rejected; every entrance remains open.";
            return true;
        }
        public bool Remove(string id)
        {
            if (!Planning) return false;
            bool removed = Draft.towers.RemoveAll(t => t.entryId == id) > 0; RebuildWalls(); return removed;
        }
        public void Reset()
        {
            Draft.started = Draft.finished = false; Draft.tick = Draft.spawned = Draft.breaches = 0;
            Draft.invaders.Clear(); Effects.Clear(); RebuildWalls(); Message = "Planning: rearrange freely; no actions spent.";
            Draft.defended = Draft.pressureCaptured = false; Draft.nextSpawnTick = 0; Draft.batchLinkIds.Clear(); PreparePressure(); RebuildWalls();
        }
        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        static float SegmentDistance(Vector2 point, Vector2 a, Vector2 b)
        {
            var direction = b - a;
            float fraction = direction.sqrMagnitude < .000001f ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, direction) / direction.sqrMagnitude);
            return Vector2.Distance(point, a + direction * fraction);
        }
        static bool OnSegment(Vector2 p, Vector2 a, Vector2 b) => Mathf.Abs(Cross(b - a, p - a)) < .001f && Vector2.Dot(p - a, p - b) <= .001f;
        static bool Intersects(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            float abC = Cross(b - a, c - a), abD = Cross(b - a, d - a), cdA = Cross(d - c, a - c), cdB = Cross(d - c, b - c);
            return abC * abD < 0 && cdA * cdB < 0 || OnSegment(a, c, d) || OnSegment(b, c, d) || OnSegment(c, a, b) || OnSegment(d, a, b);
        }
        bool Connected(Vector2 a, Vector2 b)
        {
            var seen = new HashSet<Vector2> { a }; var queue = new Queue<Vector2>(); queue.Enqueue(a);
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue(); if (cell == b) return true;
                foreach (var wall in Walls.Where(w => w.Accepted && (w.a == cell || w.b == cell)))
                { var next = wall.a == cell ? wall.b : wall.a; if (seen.Add(next)) queue.Enqueue(next); }
            }
            return false;
        }
        public void RebuildWalls()
        {
            routePoints = null;
            Walls.Clear(); var candidates = new List<FocusWall>();
            var towers = Draft.towers.OrderBy(t => t.entryId, StringComparer.Ordinal).ToList();
            for (int i = 0; i < towers.Count; i++) for (int j = i + 1; j < towers.Count; j++)
                if (Vector2.Distance(towers[i].Cell, towers[j].Cell) <= Tuning.wallRadius)
                    candidates.Add(new FocusWall { a = towers[i].Cell, b = towers[j].Cell });
            foreach (var wall in candidates.OrderBy(w => (w.a - w.b).sqrMagnitude))
            {
                if (Connected(wall.a, wall.b)) wall.rejection = "Would form a closed wall loop.";
                else if (SegmentDistance(Center, wall.a, wall.b) < .6f || towers.Any(t => t.Cell != wall.a && t.Cell != wall.b && SegmentDistance(t.Cell, wall.a, wall.b) < .35f))
                    wall.rejection = "Would pass through the center or another tower.";
                else if (Walls.Any(w => w.Accepted && w.a != wall.a && w.a != wall.b && w.b != wall.a && w.b != wall.b && Intersects(w.a, w.b, wall.a, wall.b)))
                    wall.rejection = "Would cross another wall.";
                else if (Draft.environment.Any(z => z.kind == DoubtZoneKind.Obstruction && SegmentDistance(z.position, wall.a, wall.b) < z.radius + .08f)) wall.rejection = "Would cross an obstruction.";
                Walls.Add(wall);
                routePoints = null;
                if (wall.Accepted && !RoutesOpen) { wall.rejection = "Would block a route from an entrance to the center."; routePoints = null; }
            }
        }
        static bool Inside(Vector2 p) => p.x >= -.45f && p.x <= Width - .55f && p.y >= -.45f && p.y <= Height - .55f;
        public bool SegmentOpen(Vector2 a, Vector2 b)
        {
            if (!Inside(a) || !Inside(b)) return false;
            foreach (var tower in Draft.towers) if (SegmentDistance(tower.Cell, a, b) < TowerClearance - .0001f) return false;
            foreach (var zone in Draft.environment) if (zone.kind == DoubtZoneKind.Obstruction && SegmentDistance(zone.position, a, b) < zone.radius + .18f) return false;
            foreach (var wall in Walls)
            {
                if (!wall.Accepted) continue;
                if (Intersects(a, b, wall.a, wall.b) || Mathf.Min(SegmentDistance(a, wall.a, wall.b), SegmentDistance(b, wall.a, wall.b),
                    SegmentDistance(wall.a, a, b), SegmentDistance(wall.b, a, b)) < WallClearance) return false;
            }
            return true;
        }
        void BuildRoutes()
        {
            routePoints = new List<Vector2> { Center }; routePoints.AddRange(Entrances);
            foreach (var tower in Draft.towers.OrderBy(t => t.entryId, StringComparer.Ordinal))
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    var point = tower.Cell + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * .72f;
                    if (Inside(point) && SegmentOpen(point, point)) routePoints.Add(point);
                }
            foreach (var zone in Draft.environment.Where(z => z.kind == DoubtZoneKind.Obstruction))
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4;
                    var point = zone.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (zone.radius + .36f);
                    if (Inside(point) && SegmentOpen(point, point)) routePoints.Add(point);
                }
            int n = routePoints.Count; var edges = new float[n, n];
            for (int i = 0; i < n; i++) for (int j = i + 1; j < n; j++)
                edges[i, j] = edges[j, i] = SegmentOpen(routePoints[i], routePoints[j]) ? Vector2.Distance(routePoints[i], routePoints[j]) : float.PositiveInfinity;
            routeDistance = Enumerable.Repeat(float.PositiveInfinity, n).ToArray(); routeNext = Enumerable.Repeat(-1, n).ToArray();
            var done = new bool[n]; routeDistance[0] = 0;
            for (int step = 0; step < n; step++)
            {
                int at = -1;
                for (int i = 0; i < n; i++) if (!done[i] && (at < 0 || routeDistance[i] < routeDistance[at])) at = i;
                if (at < 0 || float.IsPositiveInfinity(routeDistance[at])) break;
                done[at] = true;
                for (int next = 0; next < n; next++)
                {
                    if (next == at || done[next]) continue;
                    float distance = routeDistance[at] + edges[at, next];
                    if (distance < routeDistance[next]) { routeDistance[next] = distance; routeNext[next] = at; }
                }
            }
        }
        public List<Vector2> Path(Vector2 start)
        {
            if ((start - Center).sqrMagnitude < .000001f) return new List<Vector2> { Center };
            if (SegmentOpen(start, Center)) return new List<Vector2> { start, Center };
            if (routePoints == null) BuildRoutes();
            int best = -1; float shortest = float.PositiveInfinity;
            for (int i = 0; i < routePoints.Count; i++)
            {
                float distance = Vector2.Distance(start, routePoints[i]) + routeDistance[i];
                if (distance < shortest && SegmentOpen(start, routePoints[i])) { shortest = distance; best = i; }
            }
            var path = new List<Vector2>(); if (best < 0) return path;
            path.Add(start);
            for (int at = best; at >= 0; at = routeNext[at])
                if ((path[path.Count - 1] - routePoints[at]).sqrMagnitude > .000001f) path.Add(routePoints[at]);
            return path;
        }
        public bool Start()
        {
            RefreshPressure();
            if (State.PendingDoubts.Count == 0) { Message = "No unresolved doubt remains."; return false; }
            if (Draft.towers.Any(t => Draft.environment.Any(z => z.kind == DoubtZoneKind.Obstruction && Vector2.Distance(z.position, t.Cell) < z.radius + .35f))) { Message = "Accumulated doubt changed the board. Move towers away from obstructions."; return false; }
            if (!Planning || !State.CanBuildAccusation || FocusUsed > State.FocusCapacity || !Draft.towers.Any(t => DoubtRules.Kind(State.catalogue, t.entryId) == FocusTowerKind.Projectile))
            { Message = "Place at least one clue projectile tower within your focus budget."; return false; }
            RebuildWalls(); if (!RoutesOpen) { Message = "Every entrance needs an open route."; return false; }
            Draft.batchLinkIds = State.PendingDoubts.Select(d => d.id).ToList(); Draft.pressureCaptured = true;
            Definition.challenges = State.PendingDoubts.Select(d => State.catalogue.Theory(d.questionId).text + " / " + d.reason).ToList();
            Draft.started = true; Draft.navigationVersion = 2; Message = "Defending accumulated doubt…"; return true;
        }
        float Relevance(FocusPlacement tower) => DoubtRules.Relevant(State.catalogue, Definition, tower.entryId) ? 1.25f : 1;
        public bool Buffed(FocusPlacement tower) => Draft.towers.Any(other => other != tower && DoubtRules.Kind(State.catalogue, other.entryId) == FocusTowerKind.Buff &&
            Vector2.Distance(other.Cell, tower.Cell) <= Tuning.buffRange * Relevance(other));
        public float Range(FocusPlacement tower)
        {
            var kind = DoubtRules.Kind(State.catalogue, tower.entryId);
            return (kind == FocusTowerKind.Projectile ? Tuning.projectileRange : kind == FocusTowerKind.Buff ? Tuning.buffRange * Relevance(tower) :
                Tuning.slowRange * Relevance(tower) * (Buffed(tower) ? Tuning.buffMultiplier : 1)) * TowerEnvironmentMultiplier(tower.Cell);
        }
        public void Step()
        {
            if (!Draft.started || Draft.finished || !State.CanBuildAccusation) return;
            Effects.Clear();
            int totalInvaders = Draft.waves * Tuning.enemiesPerWave;
            if (Draft.spawned < totalInvaders && Draft.tick >= Draft.nextSpawnTick)
            {
                var cell = Entrances[Draft.spawned % Entrances.Length];
                Draft.invaders.Add(new DoubtInvader { challengeIndex = Draft.spawned % Definition.challenges.Count, x = cell.x, y = cell.y, health = Tuning.invaderHealth }); Draft.spawned++;
                Draft.nextSpawnTick = Draft.tick + Tuning.spawnInterval + (Draft.spawned % Tuning.enemiesPerWave == 0 ? Tuning.wavePausePulses : 0);
            }
            foreach (var tower in Draft.towers.Where(t => DoubtRules.Kind(State.catalogue, t.entryId) == FocusTowerKind.Projectile).OrderBy(t => t.entryId, StringComparer.Ordinal))
            {
                var target = Draft.invaders.Where(i => !i.reachedCenter && i.health > 0 && Vector2.Distance(tower.Cell, i.Cell) <= Range(tower))
                    .OrderBy(i => Vector2.Distance(i.Cell, Center)).ThenBy(i => i.challengeIndex).FirstOrDefault();
                if (target == null) continue;
                target.health -= Tuning.projectileDamage * Relevance(tower) * (Buffed(tower) ? Tuning.buffMultiplier : 1) * TowerEnvironmentMultiplier(tower.Cell);
                Effects.Add(new FocusEffect { kind = FocusTowerKind.Projectile, from = tower.Cell, to = target.Cell });
            }
            foreach (var invader in Draft.invaders.Where(i => !i.reachedCenter && i.health > 0))
            {
                var slow = Draft.towers.FirstOrDefault(t => DoubtRules.Kind(State.catalogue, t.entryId) == FocusTowerKind.Slow && Vector2.Distance(t.Cell, invader.Cell) <= Range(t));
                if (slow != null) Effects.Add(new FocusEffect { kind = FocusTowerKind.Slow, from = slow.Cell, to = invader.Cell });
                float remaining = Tuning.invaderSpeed * (slow != null ? Tuning.slowedSpeed : 1) * InvaderEnvironmentMultiplier(invader.Cell);
                var path = Path(invader.Cell);
                if (path.Count == 0) { Draft.finished = true; Message = "No safe route remains. Re-plan the defense."; return; }
                for (int next = 1; next < path.Count && remaining > .00001f; next++)
                {
                    float distance = Vector2.Distance(invader.Cell, path[next]);
                    float travel = Mathf.Min(remaining, distance); var position = Vector2.MoveTowards(invader.Cell, path[next], travel);
                    invader.x = position.x; invader.y = position.y; invader.movement += travel; remaining -= travel;
                    if ((position - Center).sqrMagnitude < .000001f) { invader.reachedCenter = true; Draft.breaches++; break; }
                }
            }
            Draft.tick++;
            Draft.finished = Draft.spawned == totalInvaders && Draft.invaders.All(i => i.health <= 0 || i.reachedCenter);
            if (Draft.finished) State.CompleteDoubtDefense(this);
            Message = Draft.finished ? $"Rehearsal complete: {Draft.breaches} doubt(s) reached the center. Return to reasoning or rearrange and retry." :
                $"Defending your question… {Draft.breaches} doubt(s) reached the center.";
        }
    }
}
