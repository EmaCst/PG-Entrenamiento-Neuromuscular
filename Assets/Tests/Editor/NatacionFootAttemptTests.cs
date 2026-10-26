using NUnit.Framework;

public class NatacionFootAttemptTests
{
    private static NatacionFootAttempt NewAttempt(float lifetime=1.8f)
    {
        var a=new NatacionFootAttempt();a.Begin(10,lifetime);return a;
    }
    [Test] public void StayingInsideDoesNotScore()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(10.2,true,true,false));
    }
    [Test] public void ExitThenEntryScoresOnlyOnce()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        a.Observe(10.3,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.Hit,a.Observe(10.4,true,true,false));
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(10.5,true,true,false));
    }
    [Test] public void OneOutsideFrameDoesNotArm()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(10.3,true,true,false));
    }
    [Test] public void RepeatedAndOldFramesDoNotArm()
    {
        var a=NewAttempt();
        a.Observe(9,true,false,true);a.Observe(10.1,true,false,true);
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(10.3,true,true,false));
    }
    [Test] public void UnreliableFramesDoNotExpireOrScore()
    {
        var a=NewAttempt(.5f);
        a.Observe(10.1,true,false,true);a.Observe(10.2,false,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(20,false,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
    [Test] public void TrackingRecoveryRequiresAnotherExit()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        a.Suspend();a.Observe(15,true,true,false);a.Observe(15.1,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(15.2,true,true,false));
        a.Observe(15.3,true,false,true);a.Observe(15.4,true,false,true);
        a.Observe(15.5,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.Hit,a.Observe(15.6,true,true,false));
    }
    [Test] public void ALongObservationGapDisarmsContact()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        a.Observe(11,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(11.1,true,true,false));
    }
    [Test] public void TimeoutIsCountedOnlyOnce()
    {
        var a=NewAttempt(.25f);
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        Assert.AreEqual(NatacionFootAttempt.Outcome.Miss,a.Observe(10.3,true,false,true));
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(10.4,true,true,false));
    }
    [Test] public void LateContactCannotOverrideTimeout()
    {
        var a=NewAttempt(.35f);
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        a.Observe(10.3,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.Miss,a.Observe(10.4,true,true,false));
    }
    [Test] public void NewAttemptRequiresANewExit()
    {
        var a=NewAttempt();
        a.Observe(10.1,true,false,true);a.Observe(10.2,true,false,true);
        a.Observe(10.3,true,true,false);a.Observe(10.4,true,true,false);
        a.Begin(11,1);
        a.Observe(11.1,true,true,false);
        Assert.AreEqual(NatacionFootAttempt.Outcome.None,a.Observe(11.2,true,true,false));
    }
}
