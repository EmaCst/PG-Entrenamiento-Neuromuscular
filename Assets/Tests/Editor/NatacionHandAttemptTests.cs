using NUnit.Framework;

public class NatacionHandAttemptTests
{
    private static NatacionHandAttempt Left(float lifetime = 1f)
    {
        var a=new NatacionHandAttempt(); a.Begin(true,10,lifetime); return a;
    }
    [Test] public void TwoNewContactsAreRequiredWithoutMinimumHold()
    {
        var a=Left();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.02,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.04,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.06,true,true,false));
    }
    [Test] public void WrongHandAndLeavingTargetBreakContact()
    {
        var a=Left();a.Observe(10.02,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.04,true,false,true));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.06,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.08,true,true,false));
    }
    [Test] public void RightHandCanBeSelected()
    {
        var a=new NatacionHandAttempt();a.Begin(false,10,1);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.02,true,false,true));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.04,true,false,true));
    }
    [Test] public void StaleDuplicateAndOutOfOrderFramesCannotConfirmContact()
    {
        var a=Left();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(9.9,true,true,false));
        a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.1,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.05,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.2,true,true,false));
    }
    [Test] public void UnreliableFrameResetsThePairAndFreezesUnobservedTime()
    {
        var a=Left();a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20,false,true,true));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20.1,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(20.2,true,true,false));
    }
    [Test] public void LongGapStartsANewPair()
    {
        var a=Left();a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(11,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(11.1,true,true,false));
    }
    [Test] public void SuspensionStartsANewPair()
    {
        var a=Left();a.Observe(10.1,true,true,false);a.Suspend();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(15,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(15.1,true,true,false));
    }
    [Test] public void ContactAfterDeadlineProducesOnlyOneMiss()
    {
        var a=Left(.15f);a.Observe(10.1,true,true,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Miss,a.Observe(10.2,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.3,true,true,false));
    }
}
