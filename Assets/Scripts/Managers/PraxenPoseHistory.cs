using System.Collections.Generic;

// Clone the envelope and lists: filtering history must never change a queued upload.
public static class PraxenPoseHistory
{
    public static SessionResultData Filter(SessionResultData source,bool glasses=false)
    {
        if(source==null) return null;
        var result=new SessionResultData { sessionId=source.sessionId,athleteId=source.athleteId,
            mode=source.mode,startedAt=source.startedAt,endedAt=source.endedAt };
        if(source.exercises!=null)
            foreach(var item in source.exercises)
                if(item!=null && Matches(item.measurementMode,NatacionMeasurement.Hands,"mediapipe_hand_screen_contact_v1",glasses)) result.exercises.Add(item);
        if(source.footExercises!=null)
            foreach(var item in source.footExercises)
                if(item!=null && Matches(item.measurementMode,NatacionMeasurement.Feet,"reference_mat_foot_contact_v1",glasses)) result.footExercises.Add(item);
        if(source.runningExercises!=null)
            foreach(var item in source.runningExercises)
                if(item!=null && Matches(item.measurementMode,NatacionMeasurement.Turns,"gyroscope_turn_response",glasses)) result.runningExercises.Add(item);
        return result;
    }
    private static bool Matches(string mode,string poseMode,string glassesMode,bool glasses)
        => glasses ? mode==glassesMode : NatacionMeasurement.Matches(mode,poseMode);
    public static int Count(SessionResultData result) => result==null?0:
        result.exercises.Count+result.footExercises.Count+result.runningExercises.Count;
    public static List<SessionResultData> Page(SessionResultData[] source,int athleteId,bool glasses=false)
    {
        var page=new List<SessionResultData>();
        if(source==null || athleteId<=0) return page;
        foreach(var session in source)
        {
            if(session==null || session.athleteId!=athleteId) continue;
            var filtered=Filter(session,glasses);
            if(Count(filtered)>0) page.Add(filtered);
        }
        return page;
    }
}

