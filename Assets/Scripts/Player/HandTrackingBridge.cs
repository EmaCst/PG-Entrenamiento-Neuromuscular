using System.Collections.Generic;
using Mediapipe.Tasks.Vision.HandLandmarker;

/// <summary>
/// Conserva una copia ligera de la punta del indice y la lateralidad de cada mano.
/// </summary>
public static class HandTrackingBridge
{
    public struct TrackedHand
    {
        public string handedness;
        public float x;
        public float y;
    }

    private static readonly object Sync = new object();
    private static readonly List<TrackedHand> LatestHands = new List<TrackedHand>(2);

    public static void Publish(HandLandmarkerResult result)
    {
        lock (Sync)
        {
            LatestHands.Clear();

            if (result.handLandmarks == null || result.handedness == null)
            {
                return;
            }

            int count = System.Math.Min(result.handLandmarks.Count, result.handedness.Count);

            for (int i = 0; i < count; i++)
            {
                var landmarks = result.handLandmarks[i].landmarks;
                var categories = result.handedness[i].categories;

                if (landmarks == null || landmarks.Count <= 8 ||
                    categories == null || categories.Count == 0)
                {
                    continue;
                }

                LatestHands.Add(new TrackedHand
                {
                    handedness = categories[0].categoryName,
                    x = landmarks[8].x,
                    y = landmarks[8].y
                });
            }
        }
    }

    public static List<TrackedHand> GetLatestHands()
    {
        lock (Sync)
        {
            return new List<TrackedHand>(LatestHands);
        }
    }

    public static void Clear()
    {
        lock (Sync)
        {
            LatestHands.Clear();
        }
    }
}
