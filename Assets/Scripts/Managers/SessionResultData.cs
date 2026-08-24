using System;
using System.Collections.Generic;

[Serializable]
public class SessionResultData
{
    public string sessionId;
    public int athleteId;
    public string mode;

    public string startedAt;
    public string endedAt;

    public List<AimLabResult> exercises =
        new List<AimLabResult>();
}