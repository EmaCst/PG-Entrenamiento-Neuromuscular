using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

public enum RequiredFoot
{
    Left,
    Right
}

public class FootTargetController : MonoBehaviour
{
    private FootExerciseManager manager;

    public void Configure(FootExerciseManager exerciseManager)
    {
        manager = exerciseManager;
    }

    public void Touch(RequiredFoot usedFoot)
    {
        manager?.TargetTouched(this, usedFoot);
    }
}

