using System;

// Preparation must restart in full if the calibrated pose is lost.
public sealed class TrainingPreparationClock
{
    private readonly double duration;
    public double Remaining { get; private set; }
    public TrainingPreparationClock(double seconds)
    {
        if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0) throw new ArgumentOutOfRangeException(nameof(seconds));
        duration=Remaining=seconds;
    }
    public void Step(double seconds, bool ready)
    {
        if(!ready) { Remaining=duration;return; }
        if(double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds<0) return;
        Remaining=Math.Max(0,Remaining-seconds);
    }
}
