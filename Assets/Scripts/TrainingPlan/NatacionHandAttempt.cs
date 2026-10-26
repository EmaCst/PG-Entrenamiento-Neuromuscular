using System;

// One attempt consumes new camera observations, never rendered frames.
public sealed class NatacionHandAttempt
{
    public enum Outcome { None, Hit, Miss }
    private bool left, resolved;
    private int contacts;
    private double lastCapture, startedAt, latestObservation;
    private float lifetime;
    public float Elapsed { get; private set; }

    public void Begin(bool useLeft, double activationTime, float seconds)
    {
        left = useLeft; latestObservation = startedAt = lastCapture = activationTime;
        lifetime = seconds; Elapsed = 0; contacts = 0; resolved = false;
    }

    public void Suspend()
    {
        contacts = 0;
        lastCapture = double.NaN;
    }

    public Outcome Observe(double capturedAt, bool reliable, bool leftTouch, bool rightTouch)
    {
        if (resolved || double.IsNaN(capturedAt) || double.IsInfinity(capturedAt) ||
            capturedAt <= startedAt || capturedAt <= latestObservation)
            return Outcome.None;
        latestObservation = capturedAt;
        if (!reliable) { Suspend(); return Outcome.None; }
        if (double.IsNaN(lastCapture))
        {
            lastCapture = capturedAt;
            return Outcome.None; // recovery frame cannot count as a contact
        }
        double delta = capturedAt - lastCapture;
        lastCapture = capturedAt;
        // A gap longer than the freshness window breaks contact continuity.
        if (delta > 0.35) { contacts = 0; return Outcome.None; }
        Elapsed += (float)delta;
        contacts = (left ? leftTouch : rightTouch) ? contacts + 1 : 0;
        if (contacts >= 2 && Elapsed >= 0.12f && Elapsed <= lifetime)
        { resolved = true; return Outcome.Hit; }
        if (Elapsed >= lifetime)
        { resolved = true; return Outcome.Miss; }
        return Outcome.None;
    }
}
