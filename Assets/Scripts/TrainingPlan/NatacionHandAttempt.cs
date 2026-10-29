using System;

// One attempt consumes new camera observations, never rendered frames.
public sealed class NatacionHandAttempt
{
    public enum Outcome { None, Hit, Miss }
    private bool left, resolved;
    private double lastCapture, startedAt, latestObservation;
    private float lifetime;
    public float Elapsed { get; private set; }

    public void Begin(bool useLeft, double activationTime, float seconds)
    {
        left = useLeft; latestObservation = startedAt = lastCapture = activationTime;
        lifetime = seconds; Elapsed = 0; resolved = false;
    }

    public void Suspend()
    {
        lastCapture = double.NaN;
    }

    public Outcome Observe(double capturedAt, bool reliable, bool leftTouch, bool rightTouch)
    {
        if (resolved || double.IsNaN(capturedAt) || double.IsInfinity(capturedAt) ||
            capturedAt <= startedAt || capturedAt <= latestObservation)
            return Outcome.None;
        latestObservation = capturedAt;
        if (!reliable) { Suspend(); return Outcome.None; }
        // Only time covered by fresh observations contributes to reaction time.
        // A new reliable recovery frame may contact; stale/duplicate frames never count.
        double delta = capturedAt - lastCapture;
        lastCapture = capturedAt;
        if (!double.IsNaN(delta) && delta <= .35) Elapsed += (float)delta;
        if ((left ? leftTouch : rightTouch) && Elapsed <= lifetime)
        { resolved = true; return Outcome.Hit; }
        if (Elapsed >= lifetime)
        { resolved = true; return Outcome.Miss; }
        return Outcome.None;
    }
}

