using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StandaloneExerciseStarter : MonoBehaviour
{
    [SerializeField] private MonoBehaviour exerciseController;

    private IEnumerator Start()
    {
        yield return null;
        if (CombinedExerciseSession.Instance != null && CombinedExerciseSession.Instance.IsRunning)
            yield break;

        exerciseController?.GetType()
            .GetMethod("StartExercise", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(exerciseController, null);

        string sceneName = SceneManager.GetActiveScene().name;
        if (!PhoneTrainingOptions.AppliesTo(sceneName)) yield break;

        yield return new WaitForSecondsRealtime(PhoneTrainingOptions.DurationFor(sceneName));
        exerciseController?.GetType()
            .GetMethod("StopExercise", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(exerciseController, null);
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
    }
}
