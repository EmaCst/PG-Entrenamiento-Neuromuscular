using System;

// Pixel coordinates, Y up. A vertical drag cancels a tap; horizontal sliders stay usable.
public sealed class PraxenSwipeGesture
{
    private bool held, captured;
    private double startX,startY,lastY;
    public bool Dragging { get; private set; }
    public bool ReleasedDrag { get; private set; }
    public double DeltaY { get; private set; }
    public void Reset() { held=captured=Dragging=ReleasedDrag=false;DeltaY=0; }
    public void Step(double x,double y,bool pressed,bool inside,double threshold)
    {
        DeltaY=0;ReleasedDrag=false;
        if(!pressed)
        { ReleasedDrag=held && Dragging;held=captured=Dragging=false;return; }
        if(!held)
        { held=true;captured=inside;startX=x;startY=lastY=y;return; }
        if(!captured) return;
        double dx=Math.Abs(x-startX),dy=Math.Abs(y-startY);
        if(!Dragging && dy>threshold && dy>dx*1.2) Dragging=true;
        if(Dragging) DeltaY=y-lastY;
        lastY=y;
    }
}
