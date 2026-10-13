using NUnit.Framework;
using P = RunningBoundaryGeometry.Point;

public class RunningBoundaryGeometryTests
{
    private static P[] Square => new[] { new P(0,0), new P(0,4), new P(4,4), new P(4,0) };
    [Test] public void ClosedAreaWorksInBothDirections()
    {
        Assert.AreEqual(16f, RunningBoundaryGeometry.Area(Square));
        var points = Square; System.Array.Reverse(points);
        Assert.IsTrue(RunningBoundaryGeometry.IsSimple(points));
        Assert.IsTrue(RunningBoundaryGeometry.Contains(points, new P(2,2), 0.5f));
    }
    [Test] public void SelfCrossingBoundaryIsRejected()
    {
        Assert.IsFalse(RunningBoundaryGeometry.IsSimple(new[] { new P(0,0), new P(3,3), new P(0,3), new P(3,0) }));
    }
    [Test] public void BoundaryMarginExcludesPointsNearWalls()
    {
        Assert.IsFalse(RunningBoundaryGeometry.Contains(Square, new P(0.2f,2), 0.5f));
        Assert.IsFalse(RunningBoundaryGeometry.Contains(Square, new P(0,2)));
        Assert.IsFalse(RunningBoundaryGeometry.Contains(Square, new P(5,2)));
    }
    [Test] public void ConcaveNotchIsOutside()
    {
        var points = new[] { new P(0,0), new P(4,0), new P(4,1), new P(1,1), new P(1,4), new P(0,4) };
        Assert.IsTrue(RunningBoundaryGeometry.IsSimple(points));
        Assert.IsFalse(RunningBoundaryGeometry.Contains(points, new P(2,2)));
        Assert.IsTrue(RunningBoundaryGeometry.TryPoint(points, 0.5f, 0.5f, 0.2f, out var target));
        Assert.IsTrue(RunningBoundaryGeometry.Contains(points, target, 0.2f));
    }
    [Test] public void NarrowAreaCannotProduceUnsafeTarget()
    {
        var points = new[] { new P(0,0), new P(0,4), new P(0.7f,4), new P(0.7f,0) };
        Assert.IsFalse(RunningBoundaryGeometry.TryPoint(points, 0.5f, 0.5f, 0.5f, out _));
    }
    [Test] public void PolygonTouchingItselfIsRejected()
    {
        var points = new[] { new P(0,0), new P(4,0), new P(4,4), new P(2,0), new P(0,4) };
        Assert.IsFalse(RunningBoundaryGeometry.IsSimple(points));
    }
}
