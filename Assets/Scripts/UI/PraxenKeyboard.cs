using UnityEngine;

// Call within the menu's OnGUI. Hit testing uses each field's current GUI space,
// so scaling and scrolling do not leave stale clickable rectangles behind.
public static class PraxenKeyboard
{
    private static bool pointerDown,insideField,inViewport;
    private static Vector2 rootPointer;
    private static TouchScreenKeyboard keyboard;
    public static void BeginGUI()
    {
        var e=Event.current;
        pointerDown=e.type==EventType.MouseDown && e.button==0;
        rootPointer=e.mousePosition;insideField=false;inViewport=true;
        RememberKeyboard();
    }
    public static void SetViewport(Rect screenRect) => inViewport=screenRect.Contains(rootPointer);
    public static void Field(Rect rect)
    {
        if(pointerDown && inViewport && rect.Contains(Event.current.mousePosition))insideField=true;
    }
    public static void EndGUI()
    {
        RememberKeyboard();
        if(pointerDown && !insideField)Dismiss();
    }
    private static void RememberKeyboard()
    {
        int id=GUIUtility.keyboardControl;
        if(id==0)return;
        var editor=GUIUtility.GetStateObject(typeof(TextEditor),id) as TextEditor;
        if(editor!=null && editor.keyboardOnScreen!=null)keyboard=editor.keyboardOnScreen;
    }
    public static void Dismiss()
    {
        if(keyboard!=null){keyboard.active=false;keyboard=null;}
        GUI.FocusControl(null);GUIUtility.keyboardControl=0;
    }
}
