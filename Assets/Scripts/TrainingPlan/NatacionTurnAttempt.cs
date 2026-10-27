using System;

// Pure Pose logic: angles are estimates relative to the calibrated frontal pose.
public sealed class NatacionTurnAttempt
{
    public enum Outcome { None, Completed, Timeout, Interrupted }
    public bool Active { get; private set; }
    public double Elapsed { get; private set; }
    public double MovementOnset { get; private set; } = -1;
    public double Progress { get; private set; }
    private double start, last, limit, threshold, heldSince;
    private int direction, heldFrames;
    public static double Delta(double reference, double current)
    { double d = (current-reference)%360; return d>180?d-360:d< -180?d+360:d; }
    public static bool Neutral(double shoulders, double hips) => Math.Abs(shoulders)<=12 && Math.Abs(hips)<=12;
    public void Begin(double now, bool left, double responseSeconds, double targetDegrees)
    {
        start=last=now; limit=responseSeconds; threshold=targetDegrees; direction=left?1:-1;
        Elapsed=Progress=0; MovementOnset=-1; heldSince=-1; heldFrames=0; Active=true;
    }
    public Outcome Interrupt(double now)
    {
        if(!Active) return Outcome.None;
        Elapsed=Math.Max(0,now-start); Active=false; return Outcome.Interrupted;
    }
    public Outcome Observe(double capturedAt, bool reliable, double shoulders, double hips)
    {
        if(!Active) return Outcome.None;
        if(!reliable || double.IsNaN(shoulders) || double.IsNaN(hips) ||
            double.IsInfinity(shoulders) || double.IsInfinity(hips)) return Interrupt(capturedAt);
        // Ignore duplicated/pre-cue frames, never score a pose captured before the cue.
        if(capturedAt<=last) return Outcome.None;
        if(capturedAt-last>.35) return Interrupt(capturedAt);
        last=capturedAt; Elapsed=capturedAt-start;
        double s=shoulders*direction,h=hips*direction;
        Progress=Math.Max(0,Math.Min(s,h));
        if(MovementOnset<0 && Progress>=10) MovementOnset=Elapsed;
        if(Elapsed>limit) { Active=false; return Outcome.Timeout; }
        if(s>=threshold && h>=threshold && Math.Abs(s-h)<=20)
        {
            if(heldFrames++==0) heldSince=capturedAt;
            if(heldFrames>=3 && capturedAt-heldSince>=.15)
            { Active=false; return Outcome.Completed; }
        }
        else { heldFrames=0; heldSince=-1; }
        if(Elapsed>=limit) { Active=false; return Outcome.Timeout; }
        return Outcome.None;
    }
}

// A fresh 3–5 s interval starts only after 0.5 s facing the calibrated reference.
public sealed class NatacionTurnCadence
{
    public bool Waiting { get; private set; }
    private double stable, elapsed, delay;
    public void Reset(int seconds) { delay=Math.Max(3,Math.Min(5,seconds)); stable=elapsed=0; Waiting=false; }
    public bool Observe(double dt, bool reliable, bool neutral)
    {
        if(!reliable || !neutral) { stable=elapsed=0; Waiting=false; return false; }
        dt=Math.Max(0,Math.Min(.35,dt));
        if(!Waiting) { stable+=dt; if(stable>=.5) Waiting=true; return false; }
        elapsed+=dt;
        return elapsed>=delay;
    }
}
