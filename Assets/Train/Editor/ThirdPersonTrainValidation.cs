using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GraduationDemo.Train;
using GraduationDemo.Narrative;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace GraduationDemo.Editor
{
    [InitializeOnLoad]
    public static class ThirdPersonTrainValidation
    {
        const string Pending = "GraduationDemo.ThirdPersonValidation";
        const string Output = "Documentation/ThirdPersonTrain";
        static ThirdPersonTrainValidation() { EditorApplication.playModeStateChanged += OnPlayMode; }

        [MenuItem("Graduation Demo/Validate Third Person Train")]
        public static void Run()
        {
            EditorSceneManager.OpenScene(ThirdPersonTrainSceneBuilder.ScenePath);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }

        static async void OnPlayMode(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            int exit = 0;
            try
            {
                Directory.CreateDirectory(Output);
                Application.runInBackground = true;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                await Task.Delay(1000);
                var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
                var rig = tour.cameraRig;
                var narrative = tour.narrative;
                var cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
                Require(cameras.Length == 7 && cameras.All(c => !c.orthographic), "Exactly seven perspective cameras");
                Require(tour.roomViews.Length == 3 && tour.cutawayShells.All(s => s.activeSelf), "Three enclosed carriages");
                Require(UnityEngine.Object.FindObjectsByType<Transform>().Count(t => t.name == "Interior ceiling") == 3, "Three visible interior ceilings");
                Require(tour.GetComponent<InvestigationNotebook>() != null && narrative.State != null, "Colleague's inventory / notebook is active");
                Finish(narrative);
                for (int room = 0; room < 3; room++)
                {
                    foreach (float side in new[] { -3f, 3f })
                    {
                        MoveTo(tour, new Vector3(room * 13 + side, 1.12f, -.7f));
                        rig.Refresh(true);
                        Camera expected = side < 0 ? rig.rightCorners[room] : rig.leftCorners[room];
                        Require(rig.ActiveCamera == expected && Camera.main == expected, "Opposite corner selected for cart " + room);
                        Require(cameras.Count(c => c.enabled) == 1 && cameras.Count(c => c.GetComponent<AudioListener>().enabled) == 1, "One rendering camera and listener");
                        Vector3 frame = expected.WorldToViewportPoint(tour.detective.transform.position + Vector3.up);
                        Require(frame.z > 0 && frame.x > 0 && frame.x < 1 && frame.y > 0 && frame.y < 1, "Detective inside fixed shot");
                        await Task.Delay(80);
                        Capture(expected, $"cart-{room + 1}-{(side < 0 ? "right" : "left")}.png");
                    }
                }
                MoveTo(tour, new Vector3(-1, 1.12f, -.7f)); rig.Refresh(true);
                Vector3 heading = rig.Movement(Vector2.up);
                MoveTo(tour, new Vector3(1, 1.12f, -.7f)); rig.Refresh(false);
                Require(!rig.UsingRightCorner && Vector3.Distance(heading, rig.Movement(Vector2.up)) < .001f, "Held movement survives camera cut");
                MoveTo(tour, new Vector3(-.1f, 1.12f, -.7f)); rig.Refresh(false);
                Require(!rig.UsingRightCorner, "Side hysteresis prevents jitter");
                rig.ResetMovement();
                Require(Vector3.Dot(heading, rig.Movement(Vector2.up)) < 0, "New input adopts the new camera");
                MoveTo(tour, new Vector3(6.2f, 1.12f, -.7f)); rig.Refresh(true);
                MoveTo(tour, new Vector3(6.55f, 1.12f, -.7f)); rig.Refresh(false);
                Require(rig.CurrentCarriage == 0, "Carriage boundary hysteresis");
                MoveTo(tour, new Vector3(6.9f, 1.12f, -.7f)); rig.Refresh(false);
                Require(rig.CurrentCarriage == 1, "Carriage boundary switches after dead zone");
                var keyboard = Keyboard.current ?? InputSystem.AddDevice<Keyboard>();
                await Press(keyboard, Key.C);
                Require(rig.Following && rig.ActiveCamera == rig.detectiveCamera, "C enables detective follow");
                foreach (int room in new[] { 0, 1, 2 })
                {
                    tour.SetView(room, true);
                    Require(rig.Following && rig.ActiveCamera == rig.detectiveCamera, "Follow mode persists across carts");
                    Vector3 pivot = tour.detective.transform.position + Vector3.up * rig.followHeight;
                    Require(Vector3.Distance(pivot, rig.detectiveCamera.transform.position) <= rig.followDistance + .01f, "Follow distance and collision boom");
                    Capture(rig.detectiveCamera, "follow-cart-" + (room + 1) + ".png");
                }
                // Put an obstruction on the boom and ensure it retracts in front of the surface.
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Vector3 eye = tour.detective.transform.position + Vector3.up * rig.followHeight;
                wall.transform.position = eye + (rig.detectiveCamera.transform.position - eye).normalized * 1.5f;
                wall.transform.localScale = Vector3.one * .5f;
                Physics.SyncTransforms(); rig.Refresh(true);
                Require(Vector3.Distance(eye, rig.detectiveCamera.transform.position) < 1.3f, "Follow camera avoids an obstruction");
                UnityEngine.Object.Destroy(wall); await Task.Delay(80);
                await Press(keyboard, Key.C);
                Require(!rig.Following && rig.ActiveCamera != rig.detectiveCamera, "C returns to the current cart corner");
                MoveTo(tour, new Vector3(-5.5f, 1.12f, -.7f));
                for (int i = 0; i < 370; i++) tour.detective.Move(new Vector3(.1f, -.02f, 0));
                Require(tour.detective.transform.position.x > 31 && tour.detective.transform.position.y > 1, "Walk through all three carriages");
                for (int i = 0; i < 370; i++) tour.detective.Move(new Vector3(-.1f, -.02f, 0));
                Require(tour.detective.transform.position.x < -5, "Return through all three carriages");
                var clues = UnityEngine.Object.FindObjectsByType<NarrativeInteraction>(FindObjectsSortMode.None).Where(b => !string.IsNullOrEmpty(b.clueId)).ToArray();
                Require(clues.Length == 10, "Ten colleague-authored clues retained");
                foreach (var clue in clues)
                {
                    MoveTo(tour, new Vector3(clue.transform.position.x, 1.12f, -.7f));
                    var observation = clue.GetComponent<TrainObservation>();
                    Require(Vector3.Distance(tour.detective.transform.position + Vector3.up, clue.transform.position) < observation.interactionRange, "Evidence reachable from aisle");
                    tour.OpenObservation(observation);
                    Require(narrative.State.Has(clue.clueId), "Clue recorded in shared inventory");
                    tour.CloseObservation();
                }
                foreach (var actor in UnityEngine.Object.FindObjectsByType<TrainActor>(FindObjectsSortMode.None).Where(a => a.locomotion == null))
                {
                    MoveTo(tour, new Vector3(actor.transform.position.x, 1.12f, -.7f));
                    Require(tour.TryTalkTo(actor.GetComponentInChildren<TrainObservation>()), "Suspect reachable");
                    Require(narrative.NotebookOpen, "Conversation uses colleague's notebook graph");
                    narrative.ToggleNotebook();
                }
                tour.SetView(0, true);
                await Press(keyboard, Key.N);
                Require(narrative.NotebookOpen, "N opens colleague's logbook");
                tour.GetComponent<InvestigationNotebook>().SelectTab(InvestigationNotebook.NotebookTab.Train);
                await Task.Delay(100);
                foreach (string roomName in tour.roomNames)
                    Require(UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Text>().Any(t => t.text == roomName), "Notebook train view includes " + roomName);
                Vector3 before = tour.detective.transform.position;
                await Press(keyboard, Key.W); await Press(keyboard, Key.Digit3); await Press(keyboard, Key.C);
                Require(Vector3.Distance(before, tour.detective.transform.position) < .01f && !rig.Following, "Logbook blocks movement, teleports and camera toggle");
                await Press(keyboard, Key.Escape);
                Require(!narrative.NotebookOpen, "Escape closes notebook");
                foreach (string id in new[] { "B-S01", "M-S06", "F-S04" }) narrative.State.Grant(id);
                while (narrative.State.InvestigationOpen) narrative.State.EndDay(out _);
                var sockets = new Dictionary<string, string>
                {
                    { AccusationReasoning.Motive, narrative.State.Construct("C06", "M-S06").id },
                    { AccusationReasoning.Opportunity, narrative.State.Construct("C09", "F-S04").id }
                };
                Require(narrative.AccuseWithSockets("Mila", sockets.Values, sockets), "Supported accusation starts finale");
                Require(tour.currentRoom == 2, "Finale stages in the third carriage");
                Finish(narrative);
                Require(narrative.State.Has("flag:resolved"), "Finale completes in three-carriage scene");
                const string result = "PASS: Seven perspective cameras; six opposite-corner shots and framing; one active camera/listener; enclosed interiors; side and carriage hysteresis; stable movement through cuts; C toggle and follow across all carts; follow obstruction avoidance; continuous three-carriage traversal both ways; ten reachable clues in shared inventory; three suspect graphs; three-room notebook map; N/Escape notebook input isolation; supported accusation and finale in the third carriage.";
                File.WriteAllText(Output + "/validation.txt", result);
                Debug.Log(result);
            }
            catch (Exception error) { exit = 1; Debug.LogException(error); File.WriteAllText(Output + "/validation.txt", "FAIL: " + error); }
            if (Application.isBatchMode) EditorApplication.Exit(exit);
            else EditorApplication.ExitPlaymode();
        }
        static void Finish(TrainNarrative narrative) { for (int i = 0; i < 80 && narrative.CurrentSequence != null; i++) narrative.Advance(); Require(narrative.CurrentSequence == null, "Opening completes"); }
        static void MoveTo(TrainTour tour, Vector3 position) { tour.detective.enabled = false; tour.detective.transform.position = position; tour.detective.enabled = true; Physics.SyncTransforms(); }
        static void Require(bool result, string message) { if (!result) throw new InvalidOperationException(message); }
        static async Task Press(Keyboard keyboard, Key key) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); await Task.Delay(100); InputSystem.QueueStateEvent(keyboard, new KeyboardState()); await Task.Delay(100); }
        static void Capture(Camera camera, string name)
        {
            var previous = camera.targetTexture; var active = RenderTexture.active;
            var texture = new RenderTexture(1280, 720, 24); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            camera.targetTexture = texture; camera.Render(); RenderTexture.active = texture;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            File.WriteAllBytes(Output + "/" + name, pixels.EncodeToPNG());
            camera.targetTexture = previous; RenderTexture.active = active;
            UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(pixels);
        }
    }
}
