using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using GraduationDemo.Narrative;
using UnityEditor;
using UnityEngine;

namespace GraduationDemo.Editor
{
    public static class NarrativeImporter
    {
        public const string Source = "Assets/Narrative/Source";
        public const string AssetPath = "Assets/Narrative/Data/Investigation.asset";
        public const string Opening = "OPENING — The real corpse", Ending = "ENDING — Mila's account";
        public static string Interview(string suspect) => suspect == "Felix" ? "Felix — Champagne alibi" : suspect == "Mila" ? "Mila — Solitary rehearsal" : "Bellamy — Corridor witness";

        [MenuItem("Graduation Demo/Narrative/Refresh Catalogue")]
        public static void Refresh()
        {
            RefreshFrom(Source);
        }
        public static void RefreshFrom(string source)
        {
            var existing = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(AssetPath);
            var staged = Build(source, existing);
            var errors = NarrativeValidation.Errors(staged);
            if (errors.Count > 0) { UnityEngine.Object.DestroyImmediate(staged); throw new InvalidDataException(string.Join("\n", errors)); }
            Directory.CreateDirectory("Assets/Narrative/Data");
            if (existing == null) { AssetDatabase.CreateAsset(staged, AssetPath); existing = staged; }
            else { EditorUtility.CopySerialized(staged, existing); UnityEngine.Object.DestroyImmediate(staged); EditorUtility.SetDirty(existing); }
            AssetDatabase.SaveAssets();
            Validate();
            Debug.Log("NARRATIVE_IMPORT_OK: " + existing.theories.Count + " theories, " + existing.routes.Count + " routes, " + existing.voices.Count + " voice lines.");
        }

        public static NarrativeCatalogue Build(string source, NarrativeCatalogue previous = null)
        {
            var c = ScriptableObject.CreateInstance<NarrativeCatalogue>();
            foreach (var r in Read(source, "clues.csv")) c.clues.Add(new ClueDefinition { id = r["Clue ID"], name = r["Clue name"], fact = r["Observable fact"] });
            foreach (var r in Read(source, "statements.csv")) c.statements.Add(new StatementDefinition { id = r["Statement ID"], speaker = r["Speaker"], claim = r["Exact notebook claim (not necessarily spoken subtitle)"], initial = r["Availability"] == "initial" });
            foreach (var r in Read(source, "theories.csv")) c.theories.Add(new TheoryDefinition { id = r["Theory ID"], clueId = r["Clue ID"], statementId = r["Statement ID"], text = r["Theory (automatic text)"], reading = r["Narrative reading"], truth = r["Truth status"] });
            foreach (var r in Read(source, "voice_lines.csv"))
                c.voices.Add(new VoiceDefinition { id = r["Voice file ID"], speaker = r["Actor"], subtitle = r["EXACT spoken line / subtitle"], expression = r["Portrait"], priority = r["Priority"].ToUpperInvariant(), recordingStatus = r["Recording status"], clip = previous == null ? null : previous.Voice(r["Voice file ID"])?.clip });
            foreach (var group in Read(source, "spoken_script.csv").GroupBy(x => x["Encounter title"]))
            {
                var sequence = new DialogueDefinition { id = group.Key, priority = group.First()["MoSCoW"].ToUpperInvariant() };
                foreach (var r in group.OrderBy(x => int.Parse(x["Beat"])))
                {
                    var v = c.Voice(r["Voice file ID"]);
                    if (v == null || v.subtitle != r["EXACT subtitle = spoken VO"] || v.speaker != r["Speaker"]) throw new InvalidDataException("Subtitle/speaker mismatch: " + r["Voice file ID"]);
                    var beat = new DialogueBeat { voiceId = v.id };
                    foreach (var reward in Rewards(r["Unlock on last beat"])) beat.grants.Add(reward);
                    sequence.beats.Add(beat);
                }
                c.sequences.Add(sequence);
            }
            // Explicit acquisition beats, rather than interpreting editorial labels as runtime commands.
            GrantOn(c, "FELI_001", "F-S01"); GrantOn(c, "FELI_003", "F-S02");
            GrantOn(c, "BELL_008", "B-S01"); GrantOn(c, "BELL_009", "B-S02");
            GrantOn(c, "MILA_001", "M-S01"); GrantOn(c, "MILA_004", "M-S02"); GrantOn(c, "MILA_005", "M-S03");
            c.Sequence(Opening).beats.Last().grants.Add("flag:investigation");
            c.Sequence(Ending).beats.Last().grants.Add("flag:resolved");
            foreach (var suspect in new[] { "Felix", "Mila", "Bellamy" })
            {
                string intro = suspect == "Felix" ? "BURR_081" : suspect == "Mila" ? "BURR_082" : "BURR_083";
                string response = suspect == "Felix" ? "FELIX_REACT_QUESTION_01" : suspect == "Mila" ? "MILA_REACT_QUESTION_01" : "BELL_REACT_QUESTION_01";
                c.sequences.Add(new DialogueDefinition { id = "Reusable/" + suspect, priority = "MUST", beats = new List<DialogueBeat> { new DialogueBeat { voiceId = intro }, new DialogueBeat { voiceId = response } } });
            }
            var routeKeys = new HashSet<string>();
            foreach (var r in Read(source, "routes.csv"))
            {
                string key = r["Tech route"] + "/" + r["Confront"];
                if (!routeKeys.Add(key)) throw new InvalidDataException("Duplicate confrontation key " + key);
                string seq = r["Scene to play"];
                if (seq == "Reusable theory reaction")
                {
                    var beats = new List<DialogueBeat>();
                    foreach (var line in r["Spoken reply / voice"].Replace("\\n", "\n").Split('\n'))
                    {
                        int colon = line.IndexOf(':');
                        if (colon < 0) throw new InvalidDataException("Invalid reusable dialogue " + key);
                        string speaker = line.Substring(0, colon).Trim(), text = line.Substring(colon + 1).Trim();
                        var matches = c.voices.Where(v => v.speaker == speaker && v.subtitle == text).ToList();
                        if (matches.Count != 1) throw new InvalidDataException("Reusable dialogue needs one exact voice match: " + key + " / " + line);
                        beats.Add(new DialogueBeat { voiceId = matches[0].id });
                    }
                    seq = "Reusable/" + string.Join("+", beats.Select(b => b.voiceId));
                    if (c.Sequence(seq) == null) c.sequences.Add(new DialogueDefinition { id = seq, priority = "MUST", beats = beats });
                }
                var route = new RouteDefinition { id = key + "/base", theoryId = r["Tech route"], suspect = r["Confront"], sequenceId = seq, repeatSequenceId = "Reusable/" + r["Confront"], priority = 0 };
                if (c.Sequence(seq) == null) throw new InvalidDataException("Unresolved route sequence " + seq);
                route.grants = c.Sequence(seq).beats.SelectMany(b => b.grants).Where(id => id != c.Theory(route.theoryId)?.statementId).Distinct().ToList();
                c.routes.Add(route);
            }
            foreach (var r in Read(source, "unlock_rules.csv").Where(x => x["Theory route"] != "ANY"))
            {
                string sequenceId = r["Sequence to play"] == "A fall she shouldn’t picture" ? "A fall she shouldn't picture" : r["Sequence to play"];
                var route = new RouteDefinition { id = r["Theory route"] + "/" + r["Suspect"] + "/first", theoryId = r["Theory route"], suspect = r["Suspect"], priority = int.Parse(r["Priority order"]), sequenceId = sequenceId, repeatSequenceId = "Reusable/" + r["Suspect"] };
                foreach (var condition in r["Condition (all required)"].Split(';'))
                {
                    var match = Regex.Match(condition.Trim(), @"^([FMB]-S\d{2}) (not collected|collected)$");
                    if (!match.Success) throw new InvalidDataException("Unsupported condition " + condition);
                    (match.Groups[2].Value == "collected" ? route.required : route.forbidden).Add(match.Groups[1].Value);
                }
                foreach (var reward in Rewards(r["Unlock once"])) route.grants.Add(reward);
                c.routes.Add(route);
            }
            // Reviewed canon corrections. Never replace a recording under an old spoken-line ID.
            CorrectVoice(c, "MILA_ALT_02", "MILA_CANON_SPATIAL_01", "Perhaps she fell. But she could hardly have reached the clock from where she fell.");
            CorrectVoice(c, "BURR_ALT_03", "BURR_CANON_SPATIAL_01", "From where she fell?");
            CorrectVoice(c, "MILA_ALT_04", "MILA_CANON_SPATIAL_02", "I was only guessing, Inspector.");
            CorrectVoice(c, "MILA_008", "MILA_CANON_FINALE_01", "I slapped her. She stumbled, and her head struck the heavy clock on her desk. I couldn't wake her.");
            c.voices.Add(new VoiceDefinition { id = "BURR_MMO_FINALE_01", speaker = "Burrows", subtitle = "The injury, your connection to the couple, and the sighting near Odette's compartment warrant an explanation.", expression = "Serious", priority = "MUST", recordingStatus = "New MMO finale bridge — needs recording", clip = previous?.Voice("BURR_MMO_FINALE_01")?.clip });
            foreach (var beat in c.Sequence(Ending).beats.Where(b => b.voiceId == "BURR_025")) beat.voiceId = "BURR_MMO_FINALE_01";
            foreach (var corrected in c.voices.Where(v => v.id.Contains("_CANON_"))) corrected.clip = previous?.Voice(corrected.id)?.clip;
            c.voices.Add(new VoiceDefinition { id = "BURR_NO_CHAT_01", speaker = "Burrows", subtitle = "Chit-chat? A waste of valuable time.", expression = "Dismissive", priority = "MUST", recordingStatus = "New player bark — needs recording", clip = previous?.Voice("BURR_NO_CHAT_01")?.clip });
            foreach (var voice in c.voices) voice.disclosesFallGeometry = new[] { "BURR_075", "BURR_076", "BURR_077" }.Contains(voice.id) || previous?.Voice(voice.id)?.disclosesFallGeometry == true;
            var clockGroup = new[] { "C02", "C07", "C10" };
            foreach (var t in c.theories)
            {
                if ((t.clueId == "C03" || t.clueId == "C02") && t.statementId == "B-S01") { t.propositions.Add("P1_REAL_INJURY"); t.observationGroup = "injury_and_crash"; }
                if (t.statementId == "F-S04" && new[] { "C02", "C03", "C04", "C09", "C10" }.Contains(t.clueId)) { t.propositions.Add("P3_MILA_PRESENCE"); t.observationGroup = "felix_corridor_sighting"; }
                if (t.statementId == "M-S05" && clockGroup.Contains(t.clueId)) { t.propositions.Add("P4_CONTRADICTORY_ACCOUNT"); t.observationGroup = "mila_spatial_slip"; }
                var old = previous?.Theory(t.id);
                if (old != null) { t.propositions = new List<string>(old.propositions); t.observationGroup = old.observationGroup; }
            }
            c.arrestRules = previous != null && previous.arrestRules.Count > 0 ? previous.arrestRules.Select(r => JsonUtility.FromJson<ArrestRule>(JsonUtility.ToJson(r))).ToList() : new List<ArrestRule> {
                new ArrestRule { id = "Physical MVP", suspect = "Mila", propositions = new List<string> { "P1_REAL_INJURY", "P3_MILA_PRESENCE", "P4_CONTRADICTORY_ACCOUNT" }, requiredStatements = new List<string> { "M-S01", "M-S02", "F-S04", "M-S05" }, requiredClues = new List<string> { "C02", "C03", "C07", "C10" }, forbiddenFlags = new List<string> { "flag:geometry_disclosed", "flag:resolved" }, endingSequence = Ending },
                new ArrestRule { id = "Alibi (needs strengthening)", suspect = "Mila", enabled = false, endingSequence = Ending },
                new ArrestRule { id = "Staging (missing right-hand observation)", suspect = "Mila", enabled = false, endingSequence = Ending }
            };
            AccusationReasoning.MigrateReviewedDefaults(c);
            return c;
        }
        static void GrantOn(NarrativeCatalogue c, string voice, string statement)
        {
            foreach (var s in c.sequences)
                foreach (var b in s.beats.Where(x => x.voiceId == voice)) if (!b.grants.Contains(statement)) b.grants.Add(statement);
        }
        static IEnumerable<string> Rewards(string value)
        {
            if (new[] { "", "None", "Initial notebook statements", "Unlock investigation", "Case resolved" }.Contains(value)) return Enumerable.Empty<string>();
            var matches = Regex.Matches(value, @"^(?:C\d{2}|[FMB]-S\d{2}|flag:[A-Za-z0-9_]+)(?: \(first time only\))?$");
            if (matches.Count != 1) throw new InvalidDataException("Unsupported reward annotation: " + value);
            return new[] { value.Split(' ')[0] };
        }
        static void CorrectVoice(NarrativeCatalogue c, string original, string id, string text)
        {
            var v = c.Voice(original);
            c.voices.Add(new VoiceDefinition { id = id, speaker = v.speaker, subtitle = text, expression = v.expression, priority = v.priority, recordingStatus = "Canon correction — needs new recording" });
            foreach (var s in c.sequences) foreach (var b in s.beats.Where(x => x.voiceId == original)) b.voiceId = id;
        }
        [MenuItem("Graduation Demo/Narrative/Validate Catalogue")]
        public static void Validate()
        {
            var c = AssetDatabase.LoadAssetAtPath<NarrativeCatalogue>(AssetPath);
            if (c == null) throw new InvalidOperationException("Refresh the narrative catalogue first.");
            var errors = NarrativeValidation.Errors(c);
            Directory.CreateDirectory("Documentation/Narrative");
            File.WriteAllText("Documentation/Narrative/Validation.txt", "Structural errors: " + errors.Count + "\n" + string.Join("\n", errors) + "\nMissing recordings:\n" + string.Join("\n", c.voices.Where(x => x.clip == null).Select(x => x.id + " / " + x.speaker + " / " + x.priority)));
            foreach (var error in errors) Debug.LogError(error);
            Debug.Log("NARRATIVE_VALIDATE: " + errors.Count + " errors; " + c.voices.Count(x => x.clip == null) + " unrecorded lines. See Documentation/Narrative/Validation.txt.");
        }
        public static List<Dictionary<string, string>> Read(string source, string file)
        {
            var rows = ParseCsv(File.ReadAllText(Path.Combine(source, file), Encoding.UTF8));
            if (rows.Count == 0) throw new InvalidDataException("Empty CSV " + file);
            var header = rows[0]; var result = new List<Dictionary<string, string>>();
            foreach (var row in rows.Skip(1))
            {
                if (row.All(string.IsNullOrWhiteSpace)) continue;
                if (row.Count != header.Count) throw new InvalidDataException("Column count in " + file);
                var values = new Dictionary<string, string>();
                for (int i = 0; i < header.Count; i++) values.Add(header[i].TrimStart('\uFEFF'), row[i]);
                result.Add(values);
            }
            return result;
        }
        public static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>(); var row = new List<string>(); var cell = new StringBuilder(); bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '"') { if (quoted && i + 1 < text.Length && text[i + 1] == '"') { cell.Append('"'); i++; } else quoted = !quoted; }
                else if (!quoted && ch == ',') { row.Add(cell.ToString()); cell.Clear(); }
                else if (!quoted && (ch == '\n' || ch == '\r')) { if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++; row.Add(cell.ToString()); cell.Clear(); rows.Add(row); row = new List<string>(); }
                else cell.Append(ch);
            }
            if (quoted) throw new InvalidDataException("Unclosed CSV quote");
            if (cell.Length > 0 || row.Count > 0) { row.Add(cell.ToString()); rows.Add(row); }
            return rows;
        }
    }
}
