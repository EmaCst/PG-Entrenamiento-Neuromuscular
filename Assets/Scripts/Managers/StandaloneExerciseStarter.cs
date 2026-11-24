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

        bool isFeet = exerciseController is FootExerciseManager || exerciseController is NatacionFeetExercise;
        if (exerciseController is FootExerciseManager feet)
        {
            yield return PraxenVoice.Instance.Say(this, "feet");
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!feet.IsReady && Time.realtimeSinceStartup < deadline) yield return null;
            if (!feet.IsReady)
            {
                Debug.LogWarning("No se pudo iniciar pies: camara o alfombrilla sin localizar.");
                SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
                yield break;
            }
        }

        if (exerciseController is RunningExerciseManager running)
        {
            while (!running.IsReady) yield return null;
        }
        if (exerciseController is INatacionExercise poseExercise)
        {
            NatacionVoiceGuide.BeginTraining();
            yield return NatacionVoiceGuide.For(exerciseController).Prepare(this);
            while (!poseExercise.IsReady) yield return null;
        }
        string sceneName = PraxenSceneRoutes.Canonical(SceneManager.GetActiveScene().name);
        var result = PraxenCloud.Instance.Begin("individual");
        var runner = gameObject.AddComponent<ExerciseSetRunner>();
        int sets = PhoneTrainingOptions.SetCount;
        float duration = PhoneTrainingOptions.DurationFor(sceneName);
        float rest = PhoneTrainingOptions.RestSeconds;
        yield return runner.Run(exerciseController, sets, duration, rest, announceExercise: !isFeet && !(exerciseController is INatacionExercise));
        if (runner.Completed)
        {
            PraxenResultCapture.Add(result, sceneName, sets, duration, rest);
            // The session is saved locally first; results are shown even
            // without internet access or while cloud sync is pending.
            PraxenCloud.Instance.Submit(result);
            yield return TrainingTransitionScreen.Wait(this, 5f,
                exerciseController is INatacionExercise ? "Entrenamiento completado" : "Quítate los lentes",
                exerciseController is INatacionExercise ? "Acércate al teléfono para ver tus resultados" : "En 5 segundos verás tus resultados");
            PraxenCompletedTraining.Present(result);
        }
        SceneManager.LoadScene("MenuTelefono", LoadSceneMode.Single);
    }
}


