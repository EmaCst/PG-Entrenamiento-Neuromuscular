#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PhoneStereoSetup
{
    [MenuItem("PG RA/Configurar vista estereoscopica para telefono")]
    public static void ConfigureStereoView()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 0)
            {
                mainCamera = cameras[0];
            }
        }

        if (mainCamera == null)
        {
            EditorUtility.DisplayDialog(
                "Vista estereoscopica",
                "No se encontro una camara en la escena.",
                "Aceptar"
            );
            return;
        }

        Undo.RecordObject(mainCamera.gameObject, "Configurar vista estereoscopica");

        PhoneStereoRig rig = mainCamera.GetComponent<PhoneStereoRig>();
        if (rig == null)
        {
            rig = Undo.AddComponent<PhoneStereoRig>(mainCamera.gameObject);
        }

        mainCamera.tag = "MainCamera";
        EditorUtility.SetDirty(mainCamera.gameObject);
        Selection.activeGameObject = mainCamera.gameObject;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EditorUtility.DisplayDialog(
            "Vista estereoscopica configurada",
            "Se agrego PhoneStereoRig a la camara principal. Al ejecutar, la escena se dividira en ojo izquierdo y ojo derecho.",
            "Aceptar"
        );
    }
}
#endif
