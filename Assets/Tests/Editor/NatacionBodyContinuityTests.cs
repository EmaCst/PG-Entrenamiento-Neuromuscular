using NUnit.Framework;

public class NatacionBodyContinuityTests
{
    [Test]
    public void LossNeedsThreeContinuousSecondsWithoutNewBodyFrame()
    {
        var body=new NatacionBodyContinuity();body.Detected(10);
        Assert.IsFalse(body.IsLost(12.999));Assert.IsTrue(body.IsLost(13));
        body.Detected(13.1);Assert.IsFalse(body.IsLost(13.1));
    }
    [Test]
    public void EveryDetectedFrameRestartsDeadlineButOldFramesDoNot()
    {
        var body=new NatacionBodyContinuity();body.Detected(0);body.Detected(2.9);
        Assert.IsFalse(body.IsLost(5.8));
        body.Detected(1);body.Detected(double.NaN);body.Detected(double.PositiveInfinity);
        Assert.IsTrue(body.IsLost(6));
    }
    [Test]
    public void FlickeringDetectionDoesNotRestartFiveSecondPreparation()
    {
        var body=new NatacionBodyContinuity();var clock=new TrainingPreparationClock(5);
        body.Detected(0);double previous=5;int fiveAnnouncements=0,lastNumber=-1;
        for(int frame=1;frame<=51;frame++)
        {
            double now=frame*.1;
            // Alternate good and bad detections. No three-second continuous gap.
            if(frame%2==0)body.Detected(now);
            clock.Step(.1,!body.IsLost(now));
            Assert.LessOrEqual(clock.Remaining,previous);previous=clock.Remaining;
            int number=(int)System.Math.Ceiling(clock.Remaining);
            if(number==5 && lastNumber!=number)fiveAnnouncements++;
            lastNumber=number;
        }
        Assert.AreEqual(0,clock.Remaining);Assert.AreEqual(1,fiveAnnouncements);
    }
    [Test]
    public void LongLossRestartsPreparationOnlyAfterThreshold()
    {
        var body=new NatacionBodyContinuity();body.Detected(0);
        var clock=new TrainingPreparationClock(5);
        clock.Step(2.9,!body.IsLost(2.9));Assert.AreEqual(2.1,clock.Remaining,1e-6);
        clock.Step(.1,!body.IsLost(3));Assert.AreEqual(5,clock.Remaining);
        body.Detected(3.1);clock.Step(.1,!body.IsLost(3.1));Assert.AreEqual(4.9,clock.Remaining,1e-6);
    }
    [Test]
    public void TurnPausesWithoutMissAndNeedsFreshConfirmationOnRecovery()
    {
        var a=new NatacionTurnAttempt();a.Begin(0,true,1,40);
        a.Observe(.1,true,0,0);a.Suspend();
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(2.9,true,45,45));
        Assert.AreEqual(.1,a.Elapsed,1e-6);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(3,true,45,45));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(3.1,true,45,45));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.Completed,a.Observe(3.2,true,45,45));
        Assert.AreEqual(.4,a.Elapsed,1e-6);
    }
}
