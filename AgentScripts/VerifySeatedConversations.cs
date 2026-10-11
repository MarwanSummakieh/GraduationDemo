using System;
using System.IO;
using System.Threading.Tasks;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifySeatedConversations
{
    public static async Task<string> Run()
    {
        var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        bool oldRun = Application.runInBackground;
        var keyboard = Keyboard.current;
        try
        {
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            int checkedNpc = 0;
            foreach (var actor in UnityEngine.Object.FindObjectsByType<TrainActor>())
            {
                if (actor.locomotion != null) continue;
                if (!actor.seated) throw new Exception(actor.role + " is not seated.");
                var item = actor.GetComponentInChildren<TrainObservation>();
                tour.SetView(Mathf.Clamp(Mathf.RoundToInt(actor.transform.position.x / 13), 0, 2), true);
                tour.detective.enabled = false;
                tour.detective.transform.position = new Vector3(actor.transform.position.x, 1.12f, -.5f);
                tour.detective.enabled = true;
                await Task.Delay(120);
                var hips = actor.bodyAnimator.GetBoneTransform(HumanBodyBones.Hips);
                if (Mathf.Abs(hips.position.y - actor.transform.position.y - actor.seatHeight) > .03f) throw new Exception("Seat anchor drift: " + actor.role);
                var foot = actor.bodyAnimator.GetBoneTransform(HumanBodyBones.LeftFoot);
                if (Mathf.Abs(foot.position.y - actor.transform.position.y - .1f) > .08f) throw new Exception("Foot placement: " + actor.role);
                if (tour.Nearby != item) throw new Exception("No conversation prompt from the aisle: " + actor.role);
                await Press(keyboard, Key.E);
                if (tour.ActiveConversation != item) throw new Exception("E did not start conversation: " + actor.role);
                if (!actor.facialAnimator.GetBool("Talk")) throw new Exception("Talking animation did not start.");
                int room = tour.currentRoom;
                await Press(keyboard, Key.Digit2);
                if (tour.CurrentResponse != item.responses[1] || tour.currentRoom != room) throw new Exception("Topic key switched carriages or failed to answer.");
                await Press(keyboard, Key.Escape);
                if (tour.ActiveConversation != null || actor.facialAnimator.GetBool("Talk")) throw new Exception("Conversation did not close.");
                checkedNpc++;
            }
            if (checkedNpc != 3) throw new Exception("Expected three seated conversations.");
            tour.SetView(0, true); tour.SnapCamera();
            Vector3 initialCamera = tour.viewCamera.transform.position;
            tour.detective.enabled = false; tour.detective.transform.position = new Vector3(9, 1.12f, -.5f); tour.detective.enabled = true;
            await Task.Delay(250);
            if (tour.viewCamera.transform.position.x <= initialCamera.x + 2 || Mathf.Abs(tour.viewCamera.transform.eulerAngles.y) > .01f) throw new Exception("Side camera did not follow horizontally.");
            if (tour.viewCamera.orthographicSize > 4.5f && tour.viewCamera.aspect > 1.6f) throw new Exception("Side camera is too distant.");
            const string result = "PASS: Three seated NPCs remain anchored with feet on the floor; E opens every conversation from the aisle; topic keys answer without changing rooms; Escape closes talking animation; orthographic side camera follows horizontally.";
            File.WriteAllText("Documentation/TrainPreviews/seated-conversations-validation.txt", result);
            return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
            InputSystem.settings.backgroundBehavior = oldBackground;
            Application.runInBackground = oldRun;
            tour.CloseObservation(); tour.SetView(0, true); tour.SnapCamera();
        }
    }
    static async Task Press(Keyboard keyboard, Key key)
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
        await Task.Delay(80);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        await Task.Delay(80);
    }
}
