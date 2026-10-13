using System.Collections;
using UnityEngine;

public sealed class ExerciseStartCountdown : MonoBehaviour
{
    public const float PreparationSeconds = 5f;
    public static IEnumerator Wait(MonoBehaviour owner, string detail = "")
    {
        yield return TrainingTransitionScreen.Wait(owner, PreparationSeconds, "Prepárate", detail);
    }
}
