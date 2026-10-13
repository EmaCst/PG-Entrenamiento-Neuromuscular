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

        if (exerciseController is FootExerciseManager feet)
        {
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!feet.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!feet.IsReady)
            {
                Debug.LogWarning("No se pudo iniciar pies: camara o alfombrilla sin localizar.");
                SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
                yield break;
            }
        }

        yield return ExerciseStartCountdown.Wait(this);

        var result = PraxenCloud.Instance.Begin("individual");

        exerciseController?.GetType()
            .GetMethod("StartExercise", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(exerciseController, null);

        string sceneName = SceneManager.GetActiveScene().name;
        if (!PhoneTrainingOptions.AppliesTo(sceneName)) yield break;

        yield return new WaitForSecondsRealtime(PhoneTrainingOptions.DurationFor(sceneName));
        exerciseController?.GetType()
            .GetMethod("StopExercise", BindingFlags.Instance | BindingFlags.Public)
            ?.Invoke(exerciseController, null);
        PraxenResultCapture.Add(result, sceneName, 1, PhoneTrainingOptions.DurationFor(sceneName), 0f);
        PraxenCloud.Instance.Submit(result);
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
    }
}

