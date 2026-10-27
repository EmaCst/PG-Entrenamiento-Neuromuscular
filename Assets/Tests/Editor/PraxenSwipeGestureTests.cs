using NUnit.Framework;

public class PraxenSwipeGestureTests
{
    [Test]
    public void TapRemainsATap()
    {
        var gesture=new PraxenSwipeGesture();gesture.Step(100,100,true,true,12);
        gesture.Step(102,105,true,true,12);Assert.IsFalse(gesture.Dragging);
        gesture.Step(102,105,false,true,12);Assert.IsFalse(gesture.ReleasedDrag);
    }
    [Test]
    public void VerticalDragOverControlCancelsItsTap()
    {
        var gesture=new PraxenSwipeGesture();gesture.Step(100,100,true,true,12);
        gesture.Step(102,125,true,true,12);
        Assert.IsTrue(gesture.Dragging);Assert.AreEqual(25,gesture.DeltaY);
        gesture.Step(102,145,true,true,12);Assert.AreEqual(20,gesture.DeltaY);
        gesture.Step(102,145,false,true,12);Assert.IsTrue(gesture.ReleasedDrag);
        gesture.Step(102,145,false,true,12);Assert.IsFalse(gesture.ReleasedDrag);
    }
    [Test]
    public void HorizontalSliderGestureDoesNotScroll()
    {
        var gesture=new PraxenSwipeGesture();gesture.Step(100,100,true,true,12);
        gesture.Step(160,105,true,true,12);Assert.IsFalse(gesture.Dragging);Assert.AreEqual(0,gesture.DeltaY);
    }
    [Test]
    public void DragStartingOutsideContentDoesNotCaptureIt()
    {
        var gesture=new PraxenSwipeGesture();gesture.Step(100,100,true,false,12);
        gesture.Step(100,200,true,true,12);Assert.IsFalse(gesture.Dragging);
        gesture.Step(100,200,false,true,12);
        gesture.Step(100,100,true,true,12);gesture.Step(100,70,true,true,12);
        Assert.IsTrue(gesture.Dragging);Assert.AreEqual(-30,gesture.DeltaY);
    }
}
