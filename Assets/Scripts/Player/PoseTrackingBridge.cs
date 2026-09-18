using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine;

/// <summary>
/// Expone una posicion estable para cada pie a partir de los landmarks de Pose.
/// Se promedian tobillo, talon y punta del pie para reducir el movimiento brusco.
/// </summary>
public static class PoseTrackingBridge
{
    public enum FootSide
    {
        Left,
        Right
    }

    public struct TrackedFoot
    {
        public FootSide side;
        public float x;
        public float y;
        public float visibility;
    }

    private static readonly object Sync = new object();
    private static readonly List<TrackedFoot> LatestFeet = new List<TrackedFoot>(2);

    public static void Publish(PoseLandmarkerResult result)
    {
        lock (Sync)
        {
            LatestFeet.Clear();

            if (result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                return;
            }

            var landmarks = result.poseLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count < 33)
            {
                return;
            }

            AddFoot(landmarks, FootSide.Left, 27, 29, 31);
            AddFoot(landmarks, FootSide.Right, 28, 30, 32);
        }
    }

    public static List<TrackedFoot> GetLatestFeet()
    {
        lock (Sync)
        {
            return new List<TrackedFoot>(LatestFeet);
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            LatestFeet.Clear();
        }
    }

    private static void AddFoot(
        IReadOnlyList<Mediapipe.Tasks.Components.Containers.NormalizedLandmark> landmarks,
        FootSide side,
        int ankleIndex,
        int heelIndex,
        int toeIndex)
    {
        var ankle = landmarks[ankleIndex];
        var heel = landmarks[heelIndex];
        var toe = landmarks[toeIndex];

        LatestFeet.Add(new TrackedFoot
        {
            side = side,
            x = (ankle.x + heel.x + toe.x) / 3f,
            y = (ankle.y + heel.y + toe.y) / 3f,
            visibility = Mathf.Min(ankle.visibility, Mathf.Min(heel.visibility, toe.visibility))
        });
    }
}
