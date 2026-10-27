using System;

// New detected Pose frames reset the deadline; rendered/stale/empty frames do not.
public sealed class NatacionBodyContinuity
{
    public const double LossSeconds = 3;
    private double latest = double.NegativeInfinity;
    public void Detected(double capturedAt)
    {
        if(!double.IsNaN(capturedAt) && !double.IsInfinity(capturedAt) && capturedAt>latest) latest=capturedAt;
    }
    public bool IsLost(double now) => now-latest>=LossSeconds;
}
