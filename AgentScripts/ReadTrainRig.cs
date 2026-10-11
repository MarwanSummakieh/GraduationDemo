using GraduationDemo.Train;
using UnityEngine;
public static class ReadTrainRig
{
    public static string Run()
    {
        string result="";
        foreach(var actor in Object.FindObjectsByType<TrainActor>())
        {
            var a=actor.bodyAnimator; a.Rebind(); a.Update(0);
            var hip=a.GetBoneTransform(HumanBodyBones.Hips);var thigh=a.GetBoneTransform(HumanBodyBones.LeftUpperLeg);var knee=a.GetBoneTransform(HumanBodyBones.LeftLowerLeg);var foot=a.GetBoneTransform(HumanBodyBones.LeftFoot);
            result+=actor.role+" hip="+(hip.position.y-actor.transform.position.y)+" thigh="+Vector3.Distance(thigh.position,knee.position)+" shin="+Vector3.Distance(knee.position,foot.position)+" foot="+(foot.position.y-actor.transform.position.y)+"\n";
        }
        return result;
    }
}
