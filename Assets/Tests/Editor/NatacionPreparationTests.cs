using NUnit.Framework;

public class NatacionPreparationTests
{
    [Test]
    public void LostPoseRestartsFullPreparation()
    {
        var clock=new TrainingPreparationClock(5);
        clock.Step(3,true); Assert.AreEqual(2,clock.Remaining);
        clock.Step(.1,false); Assert.AreEqual(5,clock.Remaining);
        clock.Step(20,false); Assert.AreEqual(5,clock.Remaining);
        clock.Step(4,true); Assert.AreEqual(1,clock.Remaining);
        clock.Step(1,true); Assert.AreEqual(0,clock.Remaining);
    }
    [Test]
    public void RestAndCompletionClampAtZeroWithoutReadinessGate()
    {
        var clock=new TrainingPreparationClock(30);
        clock.Step(29,true); Assert.AreEqual(1,clock.Remaining);
        clock.Step(2,true); Assert.AreEqual(0,clock.Remaining);
    }
    [Test]
    public void InvalidDeltaCannotAdvancePreparation()
    {
        var clock=new TrainingPreparationClock(5);
        clock.Step(double.NaN,true); clock.Step(double.PositiveInfinity,true);clock.Step(-1,true);
        Assert.AreEqual(5,clock.Remaining);
    }
    [TestCase(null)] [TestCase("")] [TestCase("gyroscope_turn_response")]
    [TestCase("pose_body_turn_response_v2")] [TestCase("POSE_BODY_TURN_RESPONSE")]
    public void UnknownOrIncompatibleMethodsAreNeverMerged(string method)
    { Assert.IsFalse(NatacionMeasurement.Matches(method,NatacionMeasurement.Turns)); }
    [Test]
    public void EachPoseMethodIsExplicitAndDistinct()
    {
        Assert.IsTrue(NatacionMeasurement.Matches(NatacionMeasurement.Turns,NatacionMeasurement.Turns));
        Assert.IsTrue(NatacionMeasurement.Matches(NatacionMeasurement.Hands,NatacionMeasurement.Hands));
        Assert.IsTrue(NatacionMeasurement.Matches(NatacionMeasurement.Feet,NatacionMeasurement.Feet));
        Assert.IsFalse(NatacionMeasurement.Matches(NatacionMeasurement.Hands,NatacionMeasurement.Feet));
        Assert.IsFalse(NatacionMeasurement.Matches(NatacionMeasurement.Feet,NatacionMeasurement.Turns));
    }
}
