using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GraduationDemo.Narrative
{
    [Serializable] public class ClueDefinition { public string id, name, fact; }
    [Serializable] public class StatementDefinition { public string id, speaker, claim; public bool initial; }
    [Serializable] public class TheoryDefinition
    {
        public string id, clueId, statementId;
        [TextArea] public string text;
        public string reading, truth;
        public List<string> propositions = new List<string>();
        public string observationGroup;
    }
    [Serializable] public class VoiceDefinition
    {
        public string id, speaker;
        [TextArea] public string subtitle;
        public AudioClip clip;
        public string expression, priority, recordingStatus;
        public bool disclosesFallGeometry;
    }
    [Serializable] public class DialogueBeat { public string voiceId; public List<string> grants = new List<string>(); }
    [Serializable] public class DialogueDefinition { public string id, priority; public List<DialogueBeat> beats = new List<DialogueBeat>(); }
    [Serializable] public class RouteDefinition
    {
        public string id, theoryId, suspect, sequenceId, repeatSequenceId;
        public int priority;
        public List<string> required = new List<string>(), forbidden = new List<string>();
        public List<string> grants = new List<string>();
    }
    [Serializable] public class ArrestRule
    {
        public string id, suspect;
        public bool enabled = true;
        public int selectionCount = 3, minimumObservationGroups = 3;
        public List<string> propositions = new List<string>();
        public List<string> requiredStatements = new List<string>();
        public List<string> requiredClues = new List<string>();
        public List<string> forbiddenFlags = new List<string>();
        public string endingSequence;
    }

    [CreateAssetMenu(menuName = "Murder Most Inconvenient/Narrative catalogue")]
    public sealed class NarrativeCatalogue : ScriptableObject
    {
        public List<ClueDefinition> clues = new List<ClueDefinition>();
        public List<StatementDefinition> statements = new List<StatementDefinition>();
        public List<TheoryDefinition> theories = new List<TheoryDefinition>();
        public List<VoiceDefinition> voices = new List<VoiceDefinition>();
        public List<DialogueDefinition> sequences = new List<DialogueDefinition>();
        public List<RouteDefinition> routes = new List<RouteDefinition>();
        public List<ArrestRule> arrestRules = new List<ArrestRule>();
        public ClueDefinition Clue(string id) => clues.Find(x => x.id == id);
        public StatementDefinition Statement(string id) => statements.Find(x => x.id == id);
        public TheoryDefinition Theory(string id) => theories.Find(x => x.id == id);
        public VoiceDefinition Voice(string id) => voices.Find(x => x.id == id);
        public DialogueDefinition Sequence(string id) => sequences.Find(x => x.id == id);
        public TheoryDefinition Pair(string clue, string statement) => theories.Find(x => x.clueId == clue && x.statementId == statement);
        public static bool IsSuspect(string id) => id == "Felix" || id == "Mila" || id == "Bellamy";
    }
}
