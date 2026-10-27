using System;

// Canonical IDs stay unchanged in plans/results; only physical scene loading changes.
public static class PraxenSceneRoutes
{
    public static string Canonical(string scene)
    {
        switch(scene)
        {
            case "GafasManos": return "TelefonoManos";
            case "GafasPies": return "TelefonoPies";
            case "GafasCorrer": return "TelefonoCorrer";
            default: return scene;
        }
    }
    public static bool IsGlasses(string scene) => scene=="GafasManos" || scene=="GafasPies" || scene=="GafasCorrer";
    public static bool IsExercise(string scene)
    { string id=Canonical(scene);return id=="TelefonoManos"||id=="TelefonoPies"||id=="TelefonoCorrer"; }
    public static string Resolve(string canonical,bool glasses)
    {
        string id=Canonical(canonical);
        if(!glasses)return id;
        switch(id)
        {
            case "TelefonoManos":return "GafasManos";
            case "TelefonoPies":return "GafasPies";
            case "TelefonoCorrer":return "GafasCorrer";
            default:return id;
        }
    }
}
