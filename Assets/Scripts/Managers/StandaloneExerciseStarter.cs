using System.Collections;
using System.Reflection;
using UnityEngine;

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
    }
}
