using NUnit.Framework;

public class NatacionPoseGeometryTests
{
    [Test] public void PalmSegmentCanTouchBetweenLandmarks()
    {
        Assert.IsTrue(NatacionPoseGeometry.SegmentTouchesCircle(0,0,1,0,.5f,.05f,.1f));
    }
    [Test] public void ContactDoesNotExtendPastTheHand()
    {
        Assert.IsFalse(NatacionPoseGeometry.SegmentTouchesCircle(0,0,1,0,1.3f,0,.1f));
    }
    [Test] public void CoincidentLandmarksAreHandled()
    {
        Assert.IsTrue(NatacionPoseGeometry.SegmentTouchesCircle(0,0,0,0,0,0,.1f));
        Assert.IsFalse(NatacionPoseGeometry.SegmentTouchesCircle(0,0,0,0,1,1,.1f));
    }
}
