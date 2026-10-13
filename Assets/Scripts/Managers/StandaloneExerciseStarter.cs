using System.Collections;
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

        string sceneName = SceneManager.GetActiveScene().name;
        var result = PraxenCloud.Instance.Begin("individual");
        var runner = gameObject.AddComponent<ExerciseSetRunner>();
        int sets = PhoneTrainingOptions.SetCount;
        float duration = PhoneTrainingOptions.DurationFor(sceneName);
        float rest = PhoneTrainingOptions.RestSeconds;
        yield return runner.Run(exerciseController, sets, duration, rest);
        if (runner.Completed)
        {
            PraxenResultCapture.Add(result, sceneName, sets, duration, rest);
            PraxenCloud.Instance.Submit(result);
        }
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
    }
}
