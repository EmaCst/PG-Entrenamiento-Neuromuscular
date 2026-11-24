using NUnit.Framework;

public class NatacionTurnAttemptTests
{
    [TestCase(true,45)]
    [TestCase(false,-45)]
    public void RequiresBothBodyAxesAndSustainedTurn(bool left,double angle)
    {
        var a=new NatacionTurnAttempt(); a.Begin(10,left,3,40);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(10.1,true,angle,0));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(10.2,true,angle,angle));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(10.3,true,angle,angle));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.Completed,a.Observe(10.4,true,angle,angle));
        Assert.AreEqual(.4,a.Elapsed,1e-6);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(10.5,true,angle,angle));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Interrupt(10.6));
    }
    [TestCase(-45,-45)] // Opposite direction.
    [TestCase(45,0)] // Shoulders only.
    [TestCase(0,45)] // Hips only.
    [TestCase(80,45)] // Incoherent body estimates.
    public void WrongOrPartialTurnTimesOut(double shoulders,double hips)
    {
        var a=new NatacionTurnAttempt();a.Begin(0,true,1,40);
        for(int i=1;i<10;i++) Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(i*.1,true,shoulders,hips));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.Timeout,a.Observe(1,true,shoulders,hips));
    }
    [Test]
    public void TrackingLossInterruptsOnceWithoutTimeout()
    {
        var a=new NatacionTurnAttempt();a.Begin(0,true,3,40);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.Interrupted,a.Observe(.1,false,0,0));
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(4,true,45,45));
    }
    [Test]
    public void GapInCameraFramesDoesNotCountAsResponse()
    {
        var a=new NatacionTurnAttempt();a.Begin(0,true,3,40);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(.4,true,45,45));
        Assert.IsTrue(a.Active);Assert.AreEqual(0,a.Elapsed);
    }
    [Test]
    public void OldAndRepeatedFramesCannotCompleteTurn()
    {
        var a=new NatacionTurnAttempt();a.Begin(10,true,3,40);
        Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(9.9,true,45,45));
        for(int i=0;i<5;i++) Assert.AreEqual(NatacionTurnAttempt.Outcome.None,a.Observe(10.1,true,45,45));
        Assert.IsTrue(a.Active);
    }
    [Test]
    public void CrossingAngleWrapPreservesAnatomicalDirection()
    {
        Assert.AreEqual(45,NatacionTurnAttempt.Delta(180,-135));
        Assert.AreEqual(-45,NatacionTurnAttempt.Delta(-180,135));
        Assert.IsFalse(NatacionTurnAttempt.Neutral(0,25));
    }
    [TestCase(1)] [TestCase(2)] [TestCase(3)]
    public void CadenceWaitsForNeutralThenFullInterval(int delay)
    {
        var c=new NatacionTurnCadence();c.Reset(delay);
        for(int i=0;i<40;i++) Assert.IsFalse(c.Observe(.25,true,false));
        Assert.IsFalse(c.Observe(.25,true,true));
        Assert.IsFalse(c.Observe(.25,true,true));
        Assert.IsTrue(c.Waiting);
        for(int i=0;i<delay*4-1;i++) Assert.IsFalse(c.Observe(.25,true,true));
        Assert.IsTrue(c.Observe(.25,true,true));
    }
    [Test]
    public void LeavingFrontOrLosingTrackingRestartsWait()
    {
        var c=new NatacionTurnCadence();c.Reset(3);
        for(int i=0;i<10;i++) Assert.IsFalse(c.Observe(.25,true,true));
        Assert.IsFalse(c.Observe(.25,false,true));
        Assert.IsFalse(c.Waiting);
        for(int i=0;i<10;i++) Assert.IsFalse(c.Observe(.25,true,true));
        Assert.IsFalse(c.Observe(.25,true,false));
        Assert.IsFalse(c.Waiting);
    }
}

