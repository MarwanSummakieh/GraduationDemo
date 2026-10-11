using GraduationDemo.Train;
using UnityEngine;
public static class CaptureTrainConversation
{
    public static string Open()
    {
        var tour=Object.FindAnyObjectByType<TrainTour>();
        tour.SetView(0,true);
        foreach(var item in TrainObservation.Active)
        {
            if(item.speaker==null || item.speaker.role!="Bubbly passenger") continue;
            tour.detective.enabled=false; tour.detective.transform.position=new Vector3(item.speaker.transform.position.x+.8f,1.12f,-.5f); tour.detective.enabled=true;
            tour.SnapCamera();
            if(!tour.TryTalkTo(item)) throw new System.Exception("Passenger was not reachable from the aisle.");
            tour.SelectTopic(1);
            return "Conversation open with seated passenger.";
        }
        throw new System.Exception("Lounge passenger missing.");
    }
}
