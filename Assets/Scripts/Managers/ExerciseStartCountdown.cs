using System.Collections;
using UnityEngine;

public sealed class ExerciseStartCountdown : MonoBehaviour
{
    public const float PreparationSeconds = 5f;
    public static IEnumerator Wait(MonoBehaviour owner, string detail = "", INatacionExercise exercise = null)
    {
        yield return TrainingTransitionScreen.Wait(owner, PreparationSeconds, "Prepárate", detail,
            exercise == null ? (System.Func<bool>)null : () => exercise.PreparationReady);
    }
}

