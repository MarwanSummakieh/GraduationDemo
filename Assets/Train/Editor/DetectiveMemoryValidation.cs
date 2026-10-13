using System;
using System.IO;
using System.Reflection;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GraduationDemo.Editor
{
    /// <summary>Executable integration checks without touching the player's notebook.</summary>
    public static class DetectiveMemoryValidation
    {
        [MenuItem("Graduation Demo/Validate Interrogation Memory")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before validation.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var setup = EditorSceneManager.GetSceneManagerSetup();
            string temporary = Path.Combine(Path.GetTempPath(), "detective-memory-" + Guid.NewGuid() + ".json");
            try
            {
                var memory = new DetectiveMemory(temporary);
                Require(memory.Entries.Count == 0, "New notebook must be empty.");
                Require(memory.Remember("npc/topic", "Passenger", "Carnival", "I came for the music.", "Lounge", true), "Record response.");
                Require(!memory.Remember("npc/topic", "Passenger", "Carnival", "I came for the music.", "Lounge", true), "Repeated answer must not duplicate.");
                memory.SetPinned(memory.Entries[0], true);
                memory.SetNote(memory.Entries[0], "Ask about the programme.\nKeep this as testimony.");
                var restored = new DetectiveMemory(temporary);
                Require(restored.Entries.Count == 1 && restored.Entries[0].pinned && restored.Entries[0].note.Contains("\n"), "Pins and notes survive reload.");
                Require(restored.Remember("npc/topic", "Passenger", "Carnival", "A revised answer.", "Lounge", true), "Changed testimony is retained separately.");
                File.WriteAllText(temporary, "not valid json");
                var recovered = new DetectiveMemory(temporary);
                Require(recovered.Entries.Count == 0 && recovered.Error != null, "Corrupt save must recover visibly.");
                Require(Directory.GetFiles(Path.GetDirectoryName(temporary), Path.GetFileName(temporary) + ".unreadable-*").Length == 1, "Original corrupt file is preserved.");

                EditorSceneManager.OpenScene(InterrogationMemorySceneBuilder.ScenePath);
                var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
                var notebook = tour.GetComponent<DetectiveNotebook>();
                Require(notebook != null && tour.notebook == notebook, "New scene has a wired notebook.");
                // Awake is a runtime callback; initialize explicitly for the edit-mode harness.
                notebook.saveFileName = Path.GetFileName(temporary);
                typeof(DetectiveNotebook).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(notebook, null);
                Require(notebook.Memory.Entries.Count == 0, "No spoilers on scene load.");
                int conversations = 0, objects = 0;
                foreach (var item in UnityEngine.Object.FindObjectsByType<TrainObservation>())
                {
                    Require(!string.IsNullOrEmpty(item.memoryId), "Scene observations have stable IDs.");
                    int before = notebook.Memory.Entries.Count;
                    tour.OpenObservation(item);
                    Require(notebook.Memory.Entries.Count == before + 1, "Opening interaction records exactly one memory.");
                    tour.OpenObservation(item);
                    Require(notebook.Memory.Entries.Count == before + 1, "Reopening interaction is deduplicated.");
                    if (item.speaker != null)
                    {
                        conversations++;
                        tour.SelectTopic(-1); tour.SelectTopic(999);
                        Require(notebook.Memory.Entries.Count == before + 1, "Invalid topic cannot record anything.");
                        tour.SelectTopic(1);
                        Require(notebook.Memory.Entries.Count == before + 2, "Only selected question gets recorded.");
                        var last = notebook.Memory.Entries[notebook.Memory.Entries.Count - 1];
                        Require(last.topic == item.topics[1] && last.text == item.responses[1] && last.testimony, "Exact question and response provenance.");
                        tour.SelectTopic(1);
                        Require(notebook.Memory.Entries.Count == before + 2, "Repeated topic is deduplicated.");
                    }
                    else { objects++; Require(!notebook.Memory.Entries[before].testimony, "Object is an observation."); }
                    tour.CloseObservation();
                }
                Require(conversations == 3 && objects == 3, "Three passengers and three objects remain playable.");
                var first = notebook.Memory.Entries[0];
                notebook.Memory.SetPinned(first, true);
                notebook.Memory.SetNote(first, "Integration note");
                notebook.Toggle();
                Require(notebook.IsOpen && tour.ActiveConversation == null, "Notebook closes conversation.");
                notebook.Close();
                var saved = new DetectiveMemory(notebook.Memory.SavePath);
                Require(saved.Entries.Count == 9 && saved.Entries[0].note == "Integration note" && saved.Entries[0].pinned, "All encountered memories persist.");
                Cleanup(notebook.Memory.SavePath);
                Directory.CreateDirectory("Documentation/TrainPreviews");
                const string result = "PASS: Empty start; exact heard responses; three passengers and three objects; no unasked topics; repeat deduplication; revised answers; invalid topic bounds; stable IDs; testimony/observation distinction; pins/notes and scene data persist; corrupt-save recovery; notebook closes conversation.";
                File.WriteAllText("Documentation/TrainPreviews/memory-validation.txt", result);
                Debug.Log("DETECTIVE_MEMORY_VALIDATION_OK: " + result);
            }
            finally
            {
                Cleanup(temporary);
                if (Array.Exists(setup, item => item.isLoaded && item.isActive))
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
                else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Memory validation failed: " + message);
        }

        private static void Cleanup(string path)
        {
            foreach (string file in Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + "*")) File.Delete(file);
        }
    }
}
