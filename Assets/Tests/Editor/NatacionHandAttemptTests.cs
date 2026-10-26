using NUnit.Framework;

public class NatacionHandAttemptTests
{
    private static NatacionHandAttempt Left(float lifetime = 1f)
    {
        var attempt = new NatacionHandAttempt();
        attempt.Begin(true, 10, lifetime);
        return attempt;
    }

    [Test] public void WrongHandCannotScore()
    {
        var a = Left();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None, a.Observe(10.1,true,false,true));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None, a.Observe(10.2,true,false,true));
    }

    [Test] public void RightHandSelectionIsRespected()
    {
        var a = new NatacionHandAttempt(); a.Begin(false,10,1);
        a.Observe(10.1,true,true,false);
        a.Observe(10.2,true,false,true);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.3,true,false,true));
    }

    [Test] public void OneTargetCannotScoreTwice()
    {
        var a = Left();
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.2,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.3,true,true,false));
    }

    [Test] public void RepeatedAndPreActivationFramesDoNotConfirmContact()
    {
        var a = Left();
        a.Observe(9.9,true,true,false);
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.1,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.2,true,true,false));
    }

    [Test] public void UnreliableDataCannotExpireTargetOrPreserveContact()
    {
        var a = Left();
        a.Observe(10.1,true,true,false);
        a.Observe(10.2,false,true,true);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20,false,true,true));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20.1,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20.2,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(20.3,true,true,false));
    }

    [Test] public void TrackingGapBreaksConsecutiveContact()
    {
        var a = Left();
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(11,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(11.1,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(11.2,true,true,false));
    }

    [Test] public void ReliableTimeoutProducesOnlyOneMiss()
    {
        var a = Left(.25f);
        a.Observe(10.1,true,false,false); a.Observe(10.2,true,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Miss,a.Observe(10.3,true,false,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.4,true,false,false));
    }

    [Test] public void ContactAfterDeadlineDoesNotScore()
    {
        var a = Left(.15f);
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Miss,a.Observe(10.2,true,true,false));
    }

    [Test] public void SuspensionFreezesTimeAndDiscardsRecoveryContact()
    {
        var a = Left();
        a.Observe(10.1,true,true,false); a.Suspend();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(15,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
    [Test] public void OutOfOrderFrameAfterSuspensionIsIgnored()
    {
        var a = Left();
        a.Observe(10.1,true,true,false);
        a.Observe(10.2,false,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.15,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
}
