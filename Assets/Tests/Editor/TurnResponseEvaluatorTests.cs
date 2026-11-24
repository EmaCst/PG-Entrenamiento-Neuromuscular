using NUnit.Framework;

public class TurnResponseEvaluatorTests
{
    private TurnResponseEvaluator Make(double baseline = 0, double angle = 90)
    { var e = new TurnResponseEvaluator(); e.Start(baseline,10,angle,15,0.15,6); return e; }
    [Test] public void CorrectRightTurnNeedsStability()
    {
        var e = Make(); Assert.AreEqual(TurnResponseEvaluator.Result.Pending,e.Update(90,11));
        Assert.AreEqual(TurnResponseEvaluator.Result.Completed,e.Update(90,11.2));
        Assert.AreEqual(1.2,e.ElapsedSeconds,0.0001);
    }
    [Test] public void WrongDirectionDoesNotCount()
    { var e = Make(); e.Update(-90,11); Assert.AreEqual(TurnResponseEvaluator.Result.Timeout,e.Update(-90,17)); }
    [Test] public void BriefCrossingDoesNotCount()
    { var e = Make(); e.Update(90,11); e.Update(130,11.05); Assert.AreEqual(TurnResponseEvaluator.Result.Pending,e.Update(90,11.2)); }
    [Test] public void LeftTurnUsesSignedRelativeAngle()
    {
        var e = Make(725,-90); e.Update(635,11);
        Assert.AreEqual(TurnResponseEvaluator.Result.Completed,e.Update(635,11.2));
    }
    [Test] public void YawDoesNotWrapAt360()
    { var e=Make(350); e.Update(440,11); Assert.AreEqual(TurnResponseEvaluator.Result.Completed,e.Update(440,11.2)); }
    [Test] public void LateTurnCannotBeatTimeout()
    { var e=Make(); Assert.AreEqual(TurnResponseEvaluator.Result.Timeout,e.Update(90,16.1)); }
    [Test] public void InterruptIsNotCompletion()
    { var e=Make(); e.Update(45,11); e.Interrupt(); Assert.AreEqual(TurnResponseEvaluator.Result.Interrupted,e.Update(90,12)); }
    [Test] public void CompletedAttemptIsCountedOnce()
    { var e=Make(); e.Update(90,11); e.Update(90,11.2); e.Update(0,12); Assert.AreEqual(1.2,e.ElapsedSeconds,0.0001); }
}
