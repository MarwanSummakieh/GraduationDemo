using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GraduationDemo.Train;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GraduationDemo.Editor
{
    [InitializeOnLoad]
    public static class DetectiveMemoryPlayValidation
    {
        private const string Pending = "GraduationDemo.MemoryValidationPending";
        private const string ValidationFile = "interrogation-play-validation.json";
        static DetectiveMemoryPlayValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        // Batch invocation deliberately omits -quit; the harness exits after Play checks.
        public static void Run()
        {
            string testPath = Path.Combine(Application.persistentDataPath, ValidationFile);
            foreach (string suffix in new[] { "", ".bak", ".tmp" })
                if (File.Exists(testPath + suffix)) File.Delete(testPath + suffix);
            DetectiveMemoryValidation.Run();
            EditorSceneManager.OpenScene(InterrogationMemorySceneBuilder.ScenePath);
            UnityEngine.Object.FindAnyObjectByType<DetectiveNotebook>().saveFileName = ValidationFile;
            EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        private static async void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            int exitCode = 0;
            string path = Path.Combine(Application.persistentDataPath, ValidationFile);
            try
            {
                Application.runInBackground = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                await Task.Delay(1000);
                var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
                var book = tour.notebook;
                var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                Require(book.Memory.Entries.Count == 0, "Runtime notebook starts empty.");
                await Press(keyboard, Key.J);
                Require(book.IsOpen, "J opens notebook.");
                Vector3 position = tour.detective.transform.position;
                await Press(keyboard, Key.D);
                await Press(keyboard, Key.Digit3);
                Require(Vector3.Distance(position, tour.detective.transform.position) < .001f && tour.currentRoom == 0, "Notebook blocks walking and carriage shortcuts.");
                await Press(keyboard, Key.Escape);
                Require(!book.IsOpen, "Escape closes notebook.");
                foreach (var item in UnityEngine.Object.FindObjectsByType<TrainObservation>())
                {
                    if (item.speaker == null) continue;
                    tour.SetView(Mathf.Clamp(Mathf.RoundToInt(item.transform.position.x / 13), 0, 2), true);
                    tour.detective.enabled = false;
                    tour.detective.transform.position = new Vector3(item.speaker.transform.position.x, 1.12f, -.5f);
                    tour.detective.enabled = true;
                    await Task.Delay(120);
                    Require(tour.Nearby == item, "Passenger reachable from aisle: " + item.speaker.role);
                    await Press(keyboard, Key.E);
                    Require(tour.ActiveConversation == item, "E starts conversation.");
                    int room = tour.currentRoom;
                    await Press(keyboard, Key.Digit2);
                    Require(tour.CurrentResponse == item.responses[1] && tour.currentRoom == room, "Topic key records answer without switching room.");
                    await Press(keyboard, Key.J);
                    Require(book.IsOpen && tour.ActiveConversation == null && !item.speaker.facialAnimator.GetBool("Talk"), "Opening notebook ends talking animation.");
                    await Press(keyboard, Key.Escape);
                }
                Require(book.Memory.Entries.Count == 6, "Only greetings and selected answers are remembered.");
                foreach (var item in UnityEngine.Object.FindObjectsByType<TrainObservation>())
                    if (item.speaker == null) tour.OpenObservation(item);
                Require(book.Memory.Entries.Count == 9, "Inspections join testimony.");
                book.Toggle();
                var selected = book.Memory.Entries[1];
                typeof(DetectiveNotebook).GetMethod("Choose", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(book, new object[] { selected });
                book.Memory.SetPinned(selected, true);
                // Verify the same filter predicate used by the rendered list.
                typeof(DetectiveNotebook).GetField("search", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, selected.text);
                Require(book.Matches(selected), "Exact response is searchable.");
                typeof(DetectiveNotebook).GetField("search", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, "no-match-34924");
                Require(!book.Matches(selected), "Search excludes unrelated entries.");
                typeof(DetectiveNotebook).GetField("search", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, "");
                typeof(DetectiveNotebook).GetField("filter", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, 2);
                Require(!book.Matches(selected), "Observation filter excludes testimony.");
                typeof(DetectiveNotebook).GetField("filter", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, 3);
                Require(book.Matches(selected), "Pinned filter includes pinned entry.");
                typeof(DetectiveNotebook).GetField("filter", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(book, 0);
                Directory.CreateDirectory("Documentation/TrainPreviews");
                await Task.Delay(500);
                ScreenCapture.CaptureScreenshot("Documentation/TrainPreviews/memory-notebook.png");
                await Task.Delay(1500);
                if (!Application.isBatchMode) await CaptureNarrowNotebook();
                book.Close();
                tour.SetView(0, true); tour.SnapCamera();
                await Task.Delay(300);
                ScreenCapture.CaptureScreenshot("Documentation/TrainPreviews/memory-exploration.png");
                await Task.Delay(1500);
                const string result = "PASS: Unity Play mode; J/Escape notebook controls; movement and carriage input blocked; E reaches all three passengers; topic keys preserve carriage and record exact answers; J ends talking animation; only heard answers saved; object inspections; search and observation/pinned filters.";
                File.WriteAllText("Documentation/TrainPreviews/memory-play-validation.txt", result);
                Debug.Log("DETECTIVE_MEMORY_PLAY_VALIDATION_OK: " + result);
            }
            catch (Exception exception) { exitCode = 1; Debug.LogException(exception); }
            finally
            {
                // This exact test slot is independent of the player's notebook.
                foreach (string suffix in new[] { "", ".bak", ".tmp" })
                    if (File.Exists(path + suffix)) File.Delete(path + suffix);
                if (Application.isBatchMode || Array.IndexOf(Environment.GetCommandLineArgs(), "-memoryValidationExit") >= 0)
                    EditorApplication.Exit(exitCode);
                else EditorApplication.ExitPlaymode();
            }
        }

        private static async Task Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            await Task.Delay(150);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(150);
        }

        private static async Task CaptureNarrowNotebook()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var gameView = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
            PropertyInfo selection = null;
            for (Type type = gameView.GetType(); type != null && selection == null; type = type.BaseType)
                selection = type.GetProperty("selectedSizeIndex", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var sizes = singleton.GetProperty("instance").GetValue(null);
            var groupType = assembly.GetType("UnityEditor.GameViewSizeGroupType");
            var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new[] { Enum.Parse(groupType, "Standalone") });
            var labels = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
            int narrow = Array.FindIndex(labels, label => label.Replace(" ", "").Contains("4:3"));
            if (narrow < 0) narrow = Array.FindIndex(labels, label => label.Contains("16:10"));
            if (narrow < 0 || selection == null) { Debug.Log("Narrow preview unavailable. Presets: " + string.Join(", ", labels) + "; selection property: " + (selection != null)); return; }
            int previous = (int)selection.GetValue(gameView);
            try
            {
                selection.SetValue(gameView, narrow); gameView.Repaint();
                await Task.Delay(600);
                ScreenCapture.CaptureScreenshot("Documentation/TrainPreviews/memory-notebook-narrow.png");
                await Task.Delay(1500);
            }
            finally { selection.SetValue(gameView, previous); gameView.Repaint(); }
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception("Memory play validation failed: " + message);
        }
    }
}
