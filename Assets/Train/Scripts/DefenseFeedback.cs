using System.Collections.Generic;
using System.Linq;
using GraduationDemo.Narrative;
using UnityEngine;

namespace GraduationDemo.Train
{
    // Transient presentation only: never changes combat, navigation, rewards or saved progress.
    public sealed class DefenseFeedback : MonoBehaviour
    {
        public enum Cue { Place, Remove, Launch, Shot, Spawn, Defeat, Breach, Win, Lose, Reject }
        public sealed class Burst
        {
            public Vector2 from, to;
            public Color color;
            public float time, duration;
            public bool projectile;
        }
        public readonly List<Burst> Bursts = new List<Burst>();
        readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        readonly Dictionary<DoubtInvader, Vector2> previous = new Dictionary<DoubtInvader, Vector2>();
        AudioSource source;
        float stepTime, stepDuration = .22f, nextShot, shakeUntil;
        public bool ReducedMotion { get; private set; }
        public float Volume { get; private set; }
        bool muted;
        public bool Muted { get => muted; set { muted = value; if (muted && source != null) source.Stop(); } }
        public Vector2 Shake => !ReducedMotion && Time.unscaledTime < shakeUntil ? new Vector2(Mathf.Sin(Time.unscaledTime * 91), Mathf.Cos(Time.unscaledTime * 73)) * 3 : Vector2.zero;
        public void Configure(InvestigationConfiguration config)
        {
            Volume = config == null ? .35f : config.defenseSoundVolume;
            ReducedMotion = config != null && config.defenseReducedMotion;
            if (source != null) return;
            source = gameObject.AddComponent<AudioSource>(); source.playOnAwake = false;
            source.spatialBlend = 0; source.ignoreListenerPause = true;
        }
        public void Clear() { Bursts.Clear(); previous.Clear(); if (source != null) source.Stop(); shakeUntil = 0; }
        public void Emit(Vector2 at, Color color, float duration = .55f)
        {
            Bursts.Add(new Burst { from = at, to = at, color = color, time = Time.unscaledTime, duration = duration });
            if (Bursts.Count > 96) Bursts.RemoveAt(0);
        }
        public void Play(Cue cue)
        {
            if (Muted || Volume <= 0 || source == null) return;
            if (cue == Cue.Shot && Time.unscaledTime < nextShot) return;
            if (cue == Cue.Shot) nextShot = Time.unscaledTime + .09f;
            if (!clips.TryGetValue(cue, out var clip)) { clip = MakeClip(cue); clips.Add(cue, clip); }
            source.PlayOneShot(clip, Volume * (cue == Cue.Shot ? .3f : .65f));
        }
        // Soft synthesized tones keep the feature self contained, with no external audio assets.
        static AudioClip MakeClip(Cue cue)
        {
            const int rate = 22050;
            float duration = cue == Cue.Win ? .65f : cue == Cue.Lose ? .5f : cue == Cue.Launch ? .35f : .13f;
            var samples = new float[Mathf.CeilToInt(rate * duration)];
            float phase = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, p = t / duration;
                float frequency = cue == Cue.Place ? 440 + 320 * p : cue == Cue.Remove ? 420 - 180 * p :
                    cue == Cue.Shot ? 900 - 650 * p : cue == Cue.Breach || cue == Cue.Reject ? 140 - 50 * p :
                    cue == Cue.Lose ? 260 - 150 * p : cue == Cue.Launch ? 220 + 440 * p :
                    cue == Cue.Win ? 440 * Mathf.Pow(2, (Mathf.Floor(p * 4) * 4) / 12f) : cue == Cue.Spawn ? 190 + 100 * p : 700 + 400 * p;
                phase += frequency * Mathf.PI * 2 / rate;
                float envelope = Mathf.Min(1, t / .012f) * Mathf.Pow(1 - p, 2);
                samples[i] = (Mathf.Sin(phase) * .7f + Mathf.Sin(phase * 2) * .15f) * envelope * .4f;
            }
            var clip = AudioClip.Create("Defense / " + cue, samples.Length, 1, rate, false); clip.SetData(samples, 0); return clip;
        }
        public Vector2 Position(DoubtInvader invader)
        {
            if (ReducedMotion || !previous.TryGetValue(invader, out var p)) return invader.Cell;
            return Vector2.Lerp(p, invader.Cell, Mathf.Clamp01((Time.unscaledTime - stepTime) / stepDuration));
        }
        public void Step(DoubtDefense defense)
        {
            previous.Clear();
            foreach (var invader in defense.Draft.invaders.Where(i => i.health > 0 && !i.reachedCenter)) previous[invader] = invader.Cell;
            int spawned = defense.Draft.spawned, breaches = defense.Draft.breaches;
            defense.Step(); stepTime = Time.unscaledTime; stepDuration = Mathf.Max(.02f, defense.Tuning.stepSeconds);
            foreach (var effect in defense.Effects.Where(e => e.kind == FocusTowerKind.Projectile))
            {
                Bursts.Add(new Burst { from = effect.from, to = effect.to, color = new Color(1, .8f, .4f), time = Time.unscaledTime, duration = .35f, projectile = true });
                Play(Cue.Shot);
            }
            foreach (var pair in previous.Where(p => p.Key.health <= 0)) { Emit(pair.Value, new Color(1, .4f, .55f)); Play(Cue.Defeat); }
            if (defense.Draft.spawned > spawned) { Emit(defense.Draft.invaders.Last().Cell, new Color(1, .35f, .45f)); Play(Cue.Spawn); }
            if (defense.Draft.breaches > breaches) { Emit(DoubtDefense.Center, new Color(1, .25f, .3f), .7f); shakeUntil = Time.unscaledTime + .25f; Play(Cue.Breach); }
            if (defense.Draft.finished)
            {
                Emit(DoubtDefense.Center, defense.Draft.defended ? new Color(.4f, 1, .75f) : new Color(1, .3f, .4f), 1.2f);
                Play(defense.Draft.defended ? Cue.Win : Cue.Lose);
            }
        }
        void Update() { Bursts.RemoveAll(b => Time.unscaledTime - b.time > b.duration); }
        void OnDestroy() { foreach (var clip in clips.Values) if (clip != null) Destroy(clip); }
    }
}
