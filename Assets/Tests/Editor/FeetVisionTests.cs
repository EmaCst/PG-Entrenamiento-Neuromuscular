using NUnit.Framework;
using UnityEngine;

public class FeetVisionTests
{
    [TestCase(8f, 6f, 0f, 0.85f, 0.48f)]
    [TestCase(25f, 16f, 0f, 0.38f, 0.24f)]
    [TestCase(35f, 12f, 0.6f, 0.85f, 0.48f)]
    [TestCase(85f, 65f, 0f, 0.9f, 0.5f)]
    public void DetectsThinOutlineInSmallDimAndPerspectiveImages(float rx, float ry, float angle, float paper, float floor)
    {
        var detector = new WhiteCircleMatDetector();
        Assert.IsTrue(detector.TryDetect(Frame(rx, ry, angle, paper, floor, 0.06f), 640, out Rect rect, out float confidence));
        Assert.GreaterOrEqual(confidence, 0.72f);
        Assert.AreEqual(319.3f, rect.center.x * 640f, 3f);
        Assert.AreEqual(350.7f, (1f - rect.center.y) * 640f, 3f);
    }

    [Test]
    public void RejectsUniformSurfaceAndSquareTile()
    {
        var detector = new WhiteCircleMatDetector();
        Assert.IsFalse(detector.TryDetect(Frame(25, 16, 0, 0.5f, 0.5f, 0.5f), 640, out _, out _));
        Assert.IsFalse(detector.TryDetect(Frame(40, 35, 0, 0.85f, 0.48f, 0.06f, true), 640, out _, out _));
        Assert.IsFalse(detector.TryDetect(null, 640, out _, out _));
    }

    [Test]
    public void CoveredMatCannotBeRetainedAgainstFeaturelessBackground()
    {
        var detector = new WhiteCircleMatDetector();
        float[] rgb = Frame(25, 16, 0, 0.85f, 0.48f, 0.06f);
        detector.TryDetect(rgb, 640, out _, out _);
        detector.TryDetect(rgb, 640, out _, out _);
        Assert.IsFalse(detector.CanRetainPrevious);
    }

    [Test]
    public void StableReferenceExpiresAndRejectsJumps()
    {
        var tracker = new MatReferenceTracker();
        var rect = new Rect(0.4f, 0.4f, 0.1f, 0.1f);
        tracker.Observe(true, rect, 0);
        tracker.Observe(true, rect, 0.1f);
        Assert.IsFalse(tracker.IsStable(0.1f));
        tracker.Observe(true, rect, 0.2f);
        Assert.IsTrue(tracker.IsStable(0.2f));
        tracker.Observe(false, default, 0.3f, true);
        Assert.IsTrue(tracker.IsStable(0.3f));
        Assert.IsFalse(tracker.IsStable(0.6f));
        tracker.Observe(true, new Rect(0.8f, 0.4f, 0.1f, 0.1f), 0.4f);
        Assert.IsFalse(tracker.IsStable(0.4f));
    }

    [Test]
    public void MovingCameraInvalidatesOcclusionImmediately()
    {
        var tracker = new MatReferenceTracker();
        var rect = new Rect(0.4f, 0.4f, 0.1f, 0.1f);
        for (int i = 0; i < 3; i++) tracker.Observe(true, rect, i * 0.1f);
        tracker.Observe(false, default, 0.3f, false);
        Assert.IsFalse(tracker.IsStable(0.3f));
    }

    [Test]
    public void FootAlreadyInsideDoesNotCount()
    {
        var gate = new FootContactGate();
        for (int i = 0; i < 8; i++) Assert.IsFalse(gate.Observe(i * 0.1f, true, true, false));
    }

    [Test]
    public void RequiresExitAndTwoDistinctContactFramesAndCountsOnlyOnce()
    {
        var gate = new FootContactGate();
        Assert.IsFalse(gate.Observe(0, true, false, true));
        Assert.IsFalse(gate.Observe(0.1f, true, false, true));
        Assert.IsFalse(gate.Observe(0.2f, true, true, false));
        Assert.IsFalse(gate.Observe(0.2f, true, true, false)); // render must not count twice
        Assert.IsTrue(gate.Observe(0.3f, true, true, false));
        Assert.IsFalse(gate.Observe(0.4f, true, true, false));
    }

    [Test]
    public void MissingObservationCannotCompleteOldContact()
    {
        var gate = new FootContactGate();
        gate.Observe(0, true, false, true);
        gate.Observe(0.1f, true, false, true);
        gate.Observe(0.2f, true, true, false);
        gate.Observe(0.3f, false, false, false);
        Assert.IsFalse(gate.Observe(0.4f, true, true, false));
        Assert.IsFalse(gate.Observe(0.5f, true, true, false));
    }

    [Test]
    public void FootBoxCornerOutsideEllipseDoesNotHit()
    {
        Rect mat = new Rect(0.4f, 0.4f, 0.2f, 0.2f);
        Assert.IsTrue(FootContactGate.Overlaps(mat, new Rect(0.46f, 0.46f, 0.08f, 0.08f)));
        Assert.IsFalse(FootContactGate.Overlaps(mat, new Rect(0.59f, 0.59f, 0.08f, 0.08f)));
        Assert.IsFalse(FootContactGate.Overlaps(mat, default));
    }

    [Test]
    public void SingleFootAcrossScreenMidpointKeepsItsTrack()
    {
        var tracker = new FootIdentityTracker();
        tracker.Assign(Box(0.4f), Box(0.8f), 2, 0, out _, out _);
        tracker.Assign(Box(0.48f), Box(0.8f), 2, 0.1f, out _, out _);
        tracker.Assign(Box(0.54f), default, 1, 0.2f, out int left, out int right);
        Assert.AreEqual(0, left);
        Assert.AreEqual(-1, right);
    }

    [Test]
    public void UnidentifiedOrAmbiguousSingleFootIsNotGuessed()
    {
        var tracker = new FootIdentityTracker();
        tracker.Assign(Box(0.3f), default, 1, 0, out int left, out int right);
        Assert.AreEqual(-1, left); Assert.AreEqual(-1, right);
        tracker.Assign(Box(0.4f), Box(0.6f), 2, 0.1f, out _, out _);
        tracker.Assign(Box(0.5f), default, 1, 0.2f, out left, out right);
        Assert.AreEqual(-1, left); Assert.AreEqual(-1, right);
    }

    private static Rect Box(float center) => new Rect(center - 0.06f, 0.3f, 0.12f, 0.25f);

    private static float[] Frame(float rx, float ry, float angle, float paper, float floor, float rim, bool square = false)
    {
        const int size = 640, plane = size * size;
        var rgb = new float[plane * 3];
        float ca = Mathf.Cos(angle), sa = Mathf.Sin(angle);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - 319.3f, dy = y + 0.5f - 350.7f;
                float u = ca * dx + sa * dy, v = -sa * dx + ca * dy;
                float inner = square ? Mathf.Max(Mathf.Abs(u) / rx, Mathf.Abs(v) / ry) : Mathf.Sqrt(u * u / (rx * rx) + v * v / (ry * ry));
                float outer = square ? Mathf.Max(Mathf.Abs(u) / (rx + 2f), Mathf.Abs(v) / (ry + 2f)) :
                    Mathf.Sqrt(u * u / ((rx + 2f) * (rx + 2f)) + v * v / ((ry + 2f) * (ry + 2f)));
                float value = inner <= 1f ? paper : (outer <= 1f ? rim : floor);
                for (int channel = 0; channel < 3; channel++) rgb[channel * plane + y * size + x] = value;
            }
        return rgb;
    }
}
