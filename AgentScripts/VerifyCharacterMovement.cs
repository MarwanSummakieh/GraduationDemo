using System;
using System.Threading.Tasks;
using GraduationDemo.Train;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class VerifyCharacterMovement
{
    public static async Task<string> Run()
    {
        var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
        var actor = tour.detective.GetComponent<TrainActor>();
        tour.SetView(0, true);
        Vector3 start = tour.detective.transform.position;
        var keyboard = Keyboard.current;
        if (keyboard == null) throw new Exception("No keyboard is available.");
        var oldInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        var oldBackground = InputSystem.settings.backgroundBehavior;
        bool oldRunInBackground = Application.runInBackground;
        try
        {
            Application.runInBackground = true;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.S));
            InputSystem.Update();
            await Task.Delay(450);
            float moved = Vector3.Distance(start, tour.detective.transform.position);
            bool walking = actor.bodyAnimator.GetBool("Walk 1");
            if (moved < .15f || !walking) throw new Exception("Movement/animation failed: distance=" + moved + " walking=" + walking);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            await Task.Delay(150);
            if (actor.bodyAnimator.GetBool("Walk 1")) throw new Exception("Walk cycle did not stop after releasing input.");
            string result = "PASS: Keyboard input moved the detective " + moved.ToString("F2") + "m, enabled walking animation, then returned to idle on release.";
            System.IO.File.WriteAllText("Documentation/TrainPreviews/character-movement-validation.txt", result);
            return result;
        }
        finally
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            InputSystem.settings.editorInputBehaviorInPlayMode = oldInput;
            InputSystem.settings.backgroundBehavior = oldBackground;
            Application.runInBackground = oldRunInBackground;
            tour.SetView(0, true);
        }
    }
}
