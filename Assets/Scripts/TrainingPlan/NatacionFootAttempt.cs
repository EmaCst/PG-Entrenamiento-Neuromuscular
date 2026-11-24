// Pure observation state: requires a reliable exit, then a new entry.
// inside/outside must refer ONLY to the requested foot in the current frame.
public sealed class NatacionFootAttempt
{
    public enum Outcome { None, Hit, Miss }
    private double latest, previous;
    private float lifetime;
    private int outsideFrames;
    private bool armed, resolved;
    public float Elapsed { get; private set; }

    public void Begin(double now, float seconds)
    {
        latest = previous = now; lifetime = seconds; Elapsed = 0;
        outsideFrames = 0; armed = resolved = false;
    }
    public void Suspend()
    {
        previous = double.NaN; outsideFrames = 0; armed = false;
    }
    public Outcome Observe(double capturedAt, bool reliable, bool inside, bool outside)
    {
        if (resolved || double.IsNaN(capturedAt) || double.IsInfinity(capturedAt) || capturedAt <= latest)
            return Outcome.None;
        latest = capturedAt;
        if (!reliable) { Suspend(); return Outcome.None; }
        if (double.IsNaN(previous)) { previous = capturedAt; return Outcome.None; }
        double delta = capturedAt - previous; previous = capturedAt;
        if (delta > .35) { outsideFrames = 0; armed = false; return Outcome.None; }
        Elapsed += (float)delta;
        if (!armed)
        {
            outsideFrames = outside && !inside ? outsideFrames + 1 : 0;
            if (outsideFrames >= 2) armed = true;
        }
        if (armed && inside && Elapsed <= lifetime)
        { resolved = true; return Outcome.Hit; }
        if (Elapsed >= lifetime) { resolved = true; return Outcome.Miss; }
        return Outcome.None;
    }
}

