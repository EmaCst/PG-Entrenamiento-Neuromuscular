using UnityEngine;

// Shared, cached single-view HUD. No generated textures or scene searches per camera frame.
public static class NatacionTrainingHud
{
    private static GUIStyle panel, title, message, metrics, timer;
    public static Rect SafeRect
    {
        get { Rect r=Screen.safeArea;r.y=Screen.height-r.yMax;return r; }
    }
    public static void Draw(string heading,string guidance,string results,string clock)
    {
        if(panel==null)
        {
            panel=new GUIStyle(GUI.skin.box);
            title=new GUIStyle(GUI.skin.label){fontStyle=FontStyle.Bold,wordWrap=false};
            message=new GUIStyle(GUI.skin.label){wordWrap=true};
            metrics=new GUIStyle(GUI.skin.label){wordWrap=true};
            timer=new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleRight,fontStyle=FontStyle.Bold,wordWrap=true};
            title.normal.textColor=timer.normal.textColor=new Color(.1f,.84f,.95f);
            message.normal.textColor=Color.white;metrics.normal.textColor=new Color(.8f,.88f,.96f);
        }
        Rect safe=SafeRect;
        float h=Mathf.Clamp(safe.height*.20f,112,190), pad=16;
        Rect area=new Rect(safe.x+12,safe.y+10,safe.width-24,h);
        Color saved=GUI.backgroundColor;
        GUI.backgroundColor=new Color(.015f,.10f,.23f,.94f);GUI.Box(area,GUIContent.none,panel);GUI.backgroundColor=saved;
        title.fontSize=Mathf.Clamp(Screen.height/42,17,27);
        message.fontSize=Mathf.Clamp(Screen.height/29,20,36);
        metrics.fontSize=Mathf.Clamp(Screen.height/43,16,26);
        timer.fontSize=Mathf.Clamp(Screen.height/29,20,36);
        float right=area.width*.26f;
        GUI.Label(new Rect(area.x+pad,area.y+7,area.width-right-pad*2,28),heading,title);
        GUI.Label(new Rect(area.x+pad,area.y+36,area.width-right-pad*2,h-66),guidance,message);
        GUI.Label(new Rect(area.x+pad,area.yMax-32,area.width-pad*2,28),results,metrics);
        GUI.Label(new Rect(area.xMax-right-pad,area.y+12,right,h-50),clock,timer);
    }
}
