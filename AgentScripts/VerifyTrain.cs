using System;
using System.IO;
using GraduationDemo.Train;
using UnityEngine;

public static class VerifyTrain
{
    public static string Run()
    {
        if (!Application.isPlaying) throw new Exception("Run this check in Play mode.");
        var tour = UnityEngine.Object.FindAnyObjectByType<TrainTour>();
        if (tour == null) throw new Exception("TrainTour is missing.");
        for (int i = 0; i < 3; i++)
        {
            tour.SetView(i, true);
            if (tour.currentRoom != i || !tour.detective.gameObject.activeSelf || tour.exterior) throw new Exception("Carriage switch failed.");
            if (Mathf.Abs(tour.detective.transform.position.x - i * 13) > .01f) throw new Exception("Carriage spawn failed.");
        }
        tour.SetExterior(true);
        foreach (var shell in tour.cutawayShells) if (!shell.activeSelf) throw new Exception("Exterior shell is missing.");
        if (tour.detective.gameObject.activeSelf) throw new Exception("Detective should be hidden in exterior view.");
        tour.SetView(0, true);
        foreach (var shell in tour.cutawayShells) if (shell.activeSelf) throw new Exception("Interior cutaway failed.");
        var controller = tour.detective;
        controller.enabled = false; controller.transform.position = new Vector3(-5.5f, 1.12f, -.5f); controller.enabled = true;
        Physics.SyncTransforms();
        for (int step = 0; step < 185; step++) controller.Move(new Vector3(.2f, -.02f, 0));
        if (controller.transform.position.x < 31.0f) throw new Exception("Forward traversal blocked at " + controller.transform.position);
        for (int step = 0; step < 185; step++) controller.Move(new Vector3(-.2f, -.02f, 0));
        if (controller.transform.position.x > -5f) throw new Exception("Return traversal blocked at " + controller.transform.position);
        if (TrainObservation.Active.Count != 6) throw new Exception("Expected three decorations and three NPC conversations.");
        var actors = UnityEngine.Object.FindObjectsByType<TrainActor>();
        if (actors.Length != 4) throw new Exception("Expected the detective and three NPCs.");
        foreach (var actor in actors)
        {
            if (actor.bodyAnimator == null || !actor.bodyAnimator.isHuman || actor.bodyAnimator.runtimeAnimatorController == null || actor.bodyAnimator.applyRootMotion)
                throw new Exception("Invalid animation setup for " + actor.role);
            if (actor.facialAnimator == null) throw new Exception("Missing facial animator for " + actor.role);
            if (actor.bodyAnimator.GetCurrentAnimatorClipInfo(0).Length == 0) throw new Exception("Body animation is not playing for " + actor.role);
        }
        foreach (var item in TrainObservation.Active)
        {
            if (item.speaker == null) continue;
            tour.OpenObservation(item);
            if (!item.speaker.facialAnimator.GetBool("Talk")) throw new Exception("Conversation animation did not start.");
            tour.CloseObservation();
            if (item.speaker.facialAnimator.GetBool("Talk")) throw new Exception("Conversation animation did not stop.");
        }
        tour.SetView(0, true);
        const string result = "PASS: Carriage switching, exterior/cutaway visibility, detective spawn, physical traversal in both directions, four package characters with body and facial animators, three decorations, and three NPC conversations with talking animation.";
        File.WriteAllText("Documentation/TrainPreviews/playmode-validation.txt", result);
        Debug.Log(result);
        return result;
    }
}
