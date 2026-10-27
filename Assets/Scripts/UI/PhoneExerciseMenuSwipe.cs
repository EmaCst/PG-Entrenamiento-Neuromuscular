using UnityEngine;
using UnityEngine.InputSystem;

public partial class PhoneExerciseMenu
{
    private readonly PraxenSwipeGesture swipe=new PraxenSwipeGesture();
    private Rect swipeViewport;
    private float swipeScale=1,portraitContentHeight,portraitViewportHeight;
    private int suppressTapUntil=-1;
    private bool touchWasActive;
    private void Update()
    {
        bool pressed=false;
        Vector2 position=Vector2.zero;
        var touchscreen=Touchscreen.current;
        bool touchActive=touchscreen!=null && touchscreen.primaryTouch.press.isPressed;
        if(touchActive || touchWasActive)
        {
            if(touchscreen!=null)position=touchscreen.primaryTouch.position.ReadValue();
            pressed=touchActive;
        }
        else if(Mouse.current!=null)
        {position=Mouse.current.position.ReadValue();pressed=Mouse.current.leftButton.isPressed;}
        touchWasActive=touchActive;
        bool inside=swipeViewport.Contains(new Vector2(position.x,Screen.height-position.y));
        swipe.Step(position.x,position.y,pressed,inside,12*swipeScale);
        if(swipe.Dragging)
            portraitScroll.y=Mathf.Clamp(portraitScroll.y+(float)swipe.DeltaY/swipeScale,0,Mathf.Max(0,portraitContentHeight-portraitViewportHeight));
        if(swipe.ReleasedDrag)suppressTapUntil=Time.frameCount+1;
    }
    private void SuppressSwipeClick()
    {
        if(swipe.Dragging || (Event.current.type==EventType.MouseUp && Time.frameCount<=suppressTapUntil))
        {
            GUIUtility.hotControl=0;GUI.FocusControl(null);
            if(Event.current.type==EventType.MouseDrag || Event.current.type==EventType.MouseUp) Event.current.Use();
        }
    }
}
