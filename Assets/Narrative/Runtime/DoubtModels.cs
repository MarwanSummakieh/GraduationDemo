using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    // Shared serialized models and rules for doubt challenges, compiled in the runtime assembly.
    public enum FocusTowerKind { Projectile, Slow, Buff }
    public enum DoubtOutcome { Unresolved, Examined, Revised, Acknowledged }
    [Serializable] public sealed class AuthoredDoubtLink
    {
        public string id, questionId, sourceId;
        [TextArea] public string reason;
        [Min(1)] public int weight = 1;
    }
    public enum DoubtZoneKind { Obstruction, InvaderBuff, TowerDebuff }
    [Serializable] public sealed class DoubtZone
    {
        public DoubtZoneKind kind;
        public Vector2 position;
        public float radius;
    }
    [Serializable] public sealed class DoubtDefinition
    {
        public string id, title, interpretation, role, clueId, statementId, questionId, sourceId;
        [TextArea] public string explanation;
        public List<string> challenges = new List<string>();
    }
    [Serializable] public sealed class DoubtTuning
    {
        [Min(1)] public int focusBudget = 6;
        [Min(0.5f)] public float wallRadius = 3.2f;
        [Min(1)] public float projectileRange = 4;
        [Min(0.1f)] public float projectileDamage = 1.8f;
        [Min(1)] public float slowRange = 3;
        [Range(.1f, 1)] public float slowedSpeed = .5f;
        [Min(1)] public float buffRange = 3;
        [Min(1)] public float buffMultiplier = 1.6f;
        [Min(.1f)] public float invaderHealth = 8;
        [Min(.1f)] public float invaderSpeed = 1;
        [Min(1)] public int spawnInterval = 3;
        [Min(.03f), Tooltip("Real seconds between automatic defense updates.")] public float stepSeconds = .22f;
        [Min(1)] public int maxWaves = 4, enemiesPerWave = 3, maxDifficulty = 6;
        [Min(0)] public int wavePausePulses = 4;
        [Range(.1f, 1)] public float environmentalTowerMultiplier = .7f;
        [Min(1)] public float environmentalInvaderSpeed = 1.3f;
        public List<string> Errors()
        {
            var errors = new List<string>();
            if (maxWaves < 1 || enemiesPerWave < 1 || maxDifficulty < 1 || wavePausePulses < 0 || environmentalTowerMultiplier <= 0 || environmentalTowerMultiplier > 1 || environmentalInvaderSpeed < 1) errors.Add("Invalid accumulated-doubt difficulty settings.");
            if (focusBudget < 1 || wallRadius < .5f || projectileRange < 1 || projectileDamage <= 0 || slowRange < 1 ||
                slowedSpeed <= 0 || slowedSpeed > 1 || buffRange < 1 || buffMultiplier < 1 || invaderHealth <= 0 || invaderSpeed <= 0 || spawnInterval < 1 || stepSeconds < .03f)
                errors.Add("Doubt focus, ranges, damage, health and speed must be positive; slow must be in (0,1], buff at least 1.");
            return errors;
        }
    }
    [Serializable] public sealed class FocusPlacement { public string entryId; public float x, y; public Vector2 Cell => new Vector2(x, y); }
    [Serializable] public sealed class DoubtInvader
    {
        public int challengeIndex;
        public float x, y;
        public float health, movement;
        public bool reachedCenter;
        public Vector2 Cell => new Vector2(x, y);
    }
    [Serializable] public sealed class DoubtDraft
    {
        public string doubtId;
        public string note = "";
        public string revisedInterpretation;
        public List<FocusPlacement> towers = new List<FocusPlacement>();
        public List<DoubtInvader> invaders = new List<DoubtInvader>();
        public int tick, spawned, breaches;
        public int navigationVersion;
        public int pressure, waves = 1, nextSpawnTick;
        public bool pressureCaptured, defended;
        public List<string> batchLinkIds = new List<string>();
        public List<DoubtZone> environment = new List<DoubtZone>();
        public bool started, finished;
    }
    [Serializable] public sealed class DoubtReview
    {
        public string doubtId, note;
        public int day, breaches;
        public bool cleared;
        public DoubtOutcome outcome;
        public List<string> entries = new List<string>();
    }
    public static class DoubtRules
    {
        public static List<AuthoredDoubtLink> AuthoredLinks() => new List<AuthoredDoubtLink>
        {
            new AuthoredDoubtLink { id = "clock:position", questionId = "T_C02_MS04", sourceId = "C10", reason = "The clock lay behind the desk, away from its usual position. Ordinary clumsiness alone may not explain that displacement." },
            new AuthoredDoubtLink { id = "clock:reach", questionId = "T_C02_MS04", sourceId = "M-S05", reason = "Mila says Odette could not reach the clock from where she fell. That challenges the suggestion that Odette simply knocked it over herself." },
            new AuthoredDoubtLink { id = "clock:impact", questionId = "T_C02_MS04", sourceId = "C07", reason = "Impact damage and displaced furniture suggest a disturbance at the desk. A routine accident may be an incomplete explanation." },
            new AuthoredDoubtLink { id = "alibi:corridor", questionId = "T_C09_MS01", sourceId = "F-S04", reason = "Felix places Mila near Odette's compartment around 20:45, challenging her claim to have rehearsed alone in her own compartment." },
            new AuthoredDoubtLink { id = "presence:alibi", questionId = "T_C04_FS04", sourceId = "M-S01", reason = "The question accepts Felix's sighting of Mila near the scene, while Mila claims she rehearsed alone elsewhere. These witness accounts conflict." }
        };
        public static IEnumerable<AuthoredDoubtLink> Links(InvestigationState state) => state.configuration?.doubtLinks ?? AuthoredLinks();
        public static List<DoubtDefinition> Defaults() => new List<DoubtDefinition>
        {
            new DoubtDefinition { id = "doubt:relationship", title = "Affection or motive?", role = AccusationReasoning.Motive,
                clueId = "C06", statementId = "F-S02", interpretation = "The relationship could provide a motive.",
                explanation = "The rings and Felix's champagne account suggest a relationship. Does knowing about it establish anyone's reason to harm Odette?",
                challenges = new List<string> { "Does your question assume jealousy before asking about the relationship?", "Could a sincere happy reaction make a different question useful?", "Would an answer clarify a reason to act, or merely establish a relationship?" } },
            new DoubtDefinition { id = "doubt:corridor", title = "Presence or opportunity?", role = AccusationReasoning.Opportunity,
                clueId = "C09", statementId = "F-S04", interpretation = "Mila's presence could establish an opportunity.",
                explanation = "The no-interruption rule and Felix's corridor sighting place a claim about Mila beside the rehearsal arrangements. Is that a chance to act, or merely presence nearby?",
                challenges = new List<string> { "Is your question distinguishing presence from a chance to act?", "Does the question assume the sighting is reliable?", "Could an innocent explanation be worth asking about too?" } },
            new DoubtDefinition { id = "doubt:clock", title = "A reliable time?", role = AccusationReasoning.Opportunity,
                clueId = "C02", statementId = "M-S03", interpretation = "The stopped clock could constrain the sequence of events.",
                explanation = "The broken clock and Mila's account invite a reconstruction. Can a damaged object establish when an event happened?",
                challenges = new List<string> { "Does your question assume when the clock stopped?", "Why connect a damaged clock to an account of someone's mood?", "Could an answer distinguish a useful connection from coincidence?" } }
        };
        public static IEnumerable<DoubtDefinition> Available(InvestigationState state, bool includeDefended = false)
        {
            foreach (var link in Links(state))
            {
                var id = link.questionId; var q = state.catalogue.Theory(id);
                if (q == null || !state.progress.theories.Contains(id) || !state.Has(link.sourceId) || !includeDefended && state.progress.defendedDoubtLinks.Contains(link.id)) continue;
                string role = q.propositions.FirstOrDefault(AccusationReasoning.Roles.Contains) ??
                    state.catalogue.InterpretationTags(q.clueId).Concat(state.catalogue.InterpretationTags(q.statementId)).FirstOrDefault(AccusationReasoning.Roles.Contains);
                yield return new DoubtDefinition { id = link.id, questionId = id, sourceId = link.sourceId,
                    title = "Doubt / " + ReasoningGraphLayout.QuestionLabel(state.progress, id), interpretation = q.text,
                    clueId = q.clueId, statementId = q.statementId, role = role, explanation = link.reason,
                    challenges = new List<string> { link.reason } };
            }
        }
        public static FocusTowerKind Kind(NarrativeCatalogue catalogue, string id) => catalogue.Clue(id) != null ? FocusTowerKind.Projectile :
            catalogue.Statement(id) != null ? FocusTowerKind.Slow : FocusTowerKind.Buff;
        public static int Cost(NarrativeCatalogue catalogue, string id) => Kind(catalogue, id) == FocusTowerKind.Buff ? 2 : 1;
        public static bool Relevant(NarrativeCatalogue catalogue, DoubtDefinition doubt, string id)
        {
            var q = catalogue.Theory(id);
            return id == doubt.sourceId || id == doubt.clueId || id == doubt.statementId || q != null && (q.clueId == doubt.clueId || q.statementId == doubt.statementId);
        }
        public static string TowerDescription(NarrativeCatalogue catalogue, DoubtDefinition doubt, DoubtTuning tuning, string id)
        {
            var kind = Kind(catalogue, id);
            string effect = kind == FocusTowerKind.Projectile ? $"Projectile: {tuning.projectileDamage:0.#} damage per pulse; range {tuning.projectileRange:0.#}." :
                kind == FocusTowerKind.Slow ? $"Area debuff: movement ×{tuning.slowedSpeed:0.##}; radius {tuning.slowRange:0.#}." :
                $"Area buff: nearby attack damage and slow radius ×{tuning.buffMultiplier:0.#}; radius {tuning.buffRange:0.#}. Buffs do not stack.";
            return id + " / " + kind + " / Focus " + Cost(catalogue, id) + "\n" + effect +
                (Relevant(catalogue, doubt, id) ? "\nRelated source: damage or effect radius ×1.25." : "\nContext from a different line of inquiry.") + "\n" + catalogue.EntryText(id);
        }
        public static TheoryDefinition Suggestion(InvestigationState state, DoubtDefinition doubt)
        {
            return state.catalogue.theories.Where(q => (q.clueId == doubt.clueId || q.statementId == doubt.statementId) &&
                state.progress.clues.Contains(q.clueId) && state.progress.statements.Contains(q.statementId) && !state.progress.theories.Contains(q.id) && !state.progress.removedTheories.Contains(q.id))
                .OrderBy(q => q.id, StringComparer.Ordinal).FirstOrDefault();
        }
    }
}
