using System;

// Relative, signed yaw in degrees. No world position, ground plane or compass is involved.
public sealed class TurnResponseEvaluator
{
    public enum Result { Pending, Completed, Timeout, Interrupted }
    private double baseline, startedAt, enteredAt = -1, target, tolerance, hold, window;
    public Result Outcome { get; private set; }
    public double ProgressDegrees { get; private set; }
    public double ElapsedSeconds { get; private set; }
    public double MovementOnsetSeconds { get; private set; } = -1;
    public void Start(double yaw, double time, double signedAngle, double toleranceDegrees, double holdSeconds, double responseSeconds)
    {
        if (Math.Abs(signedAngle) < 1 || toleranceDegrees <= 0 || toleranceDegrees >= Math.Abs(signedAngle) || holdSeconds < 0 || responseSeconds <= holdSeconds)
            throw new ArgumentOutOfRangeException(nameof(signedAngle));
        baseline = yaw; startedAt = time; target = signedAngle; tolerance = toleranceDegrees; hold = holdSeconds; window = responseSeconds;
        enteredAt = -1; Outcome = Result.Pending; ProgressDegrees = 0; ElapsedSeconds = 0; MovementOnsetSeconds = -1;
    }
    public Result Update(double yaw, double time)
    {
        if (Outcome != Result.Pending) return Outcome;
        if (double.IsNaN(yaw) || double.IsInfinity(yaw) || double.IsNaN(time) || double.IsInfinity(time)) return Interrupt();
        ElapsedSeconds = Math.Max(0, time - startedAt); ProgressDegrees = yaw - baseline;
        if (MovementOnsetSeconds < 0 && Math.Abs(ProgressDegrees) >= 5) MovementOnsetSeconds = ElapsedSeconds;
        if (ElapsedSeconds > window) { Outcome = Result.Timeout; return Outcome; }
        if (Math.Abs(ProgressDegrees - target) <= tolerance)
        {
            if (enteredAt < 0) enteredAt = time;
            if (time - enteredAt >= hold) Outcome = Result.Completed;
        }
        else enteredAt = -1;
        return Outcome;
    }
    public Result Interrupt() { if (Outcome == Result.Pending) Outcome = Result.Interrupted; return Outcome; }
}
