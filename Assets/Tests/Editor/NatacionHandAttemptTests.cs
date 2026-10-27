using NUnit.Framework;

public class NatacionHandAttemptTests
{
    private static NatacionHandAttempt Left(float lifetime = 1f)
    {
        var attempt = new NatacionHandAttempt(); attempt.Begin(true,10,lifetime); return attempt;
    }
    [Test] public void FirstReliableContactScoresWithoutMinimumHold()
    {
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,Left().Observe(10.02,true,true,false));
    }
    [Test] public void WrongHandCannotScore()
    {
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,Left().Observe(10.1,true,false,true));
        var a=new NatacionHandAttempt();a.Begin(false,10,1);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.02,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.04,true,false,true));
    }
    [Test] public void OneTargetCannotScoreTwice()
    {
        var a=Left(); Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.02,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.04,true,true,false));
    }
    [Test] public void RepeatedPreActivationAndOutOfOrderFramesAreIgnored()
    {
        var a=Left();
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(9.9,true,true,false));
        a.Observe(10.1,true,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.1,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.05,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(10.2,true,true,false));
    }
    [Test] public void UnreliableContactCannotScoreButFirstFreshRecoveryCan()
    {
        var a=Left();a.Observe(10.1,true,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(20,false,true,true));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(20.1,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
    [Test] public void GapDoesNotAddUnobservedTimeOrRequireAnotherContact()
    {
        var a=Left();a.Observe(10.1,true,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(11,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
    [Test] public void ReliableTimeoutProducesOnlyOneMiss()
    {
        var a=Left(.15f);a.Observe(10.1,true,false,false);
        Assert.AreEqual(NatacionHandAttempt.Outcome.Miss,a.Observe(10.2,true,true,false));
        Assert.AreEqual(NatacionHandAttempt.Outcome.None,a.Observe(10.3,true,true,false));
    }
    [Test] public void SuspensionAllowsFirstNewReliableContact()
    {
        var a=Left();a.Observe(10.1,true,false,false);a.Suspend();
        Assert.AreEqual(NatacionHandAttempt.Outcome.Hit,a.Observe(15,true,true,false));
        Assert.That(a.Elapsed,Is.EqualTo(.1f).Within(.0001f));
    }
}
