using System;
using Mediapipe.Tasks.Vision.HandLandmarker;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  /// <summary>
  /// Copia solo los datos que necesita el ejercicio para que puedan consumirse
  /// de forma segura desde el hilo principal de Unity.
  /// </summary>
  public static class HandTrackingBridge
  {
    public readonly struct TrackedHand
    {
      public readonly float x;
      public readonly float y;
      public readonly string handedness;

      public TrackedHand(float x, float y, string handedness)
      {
        this.x = x;
        this.y = y;
        this.handedness = handedness;
      }
    }

    private const int IndexFingerTip = 8;
    private static readonly object Sync = new object();
    private static TrackedHand[] latestHands = Array.Empty<TrackedHand>();

    public static void Publish(HandLandmarkerResult result)
    {
      if (result.handLandmarks == null)
      {
        Clear();
        return;
      }

      var hands = new TrackedHand[result.handLandmarks.Count];

      for (var i = 0; i < result.handLandmarks.Count; i++)
      {
        var landmarks = result.handLandmarks[i].landmarks;
        if (landmarks == null || landmarks.Count <= IndexFingerTip)
        {
          continue;
        }

        var fingertip = landmarks[IndexFingerTip];
        var label = string.Empty;

        if (result.handedness != null &&
            i < result.handedness.Count &&
            result.handedness[i].categories != null &&
            result.handedness[i].categories.Count > 0)
        {
          label = result.handedness[i].categories[0].categoryName;
        }

        hands[i] = new TrackedHand(fingertip.x, fingertip.y, label);
      }

      lock (Sync)
      {
        latestHands = hands;
      }
    }

    public static TrackedHand[] GetLatestHands()
    {
      lock (Sync)
      {
        // El arreglo publicado nunca vuelve a modificarse. Devolver la misma
        // referencia evita crear memoria nueva en cada frame.
        return latestHands;
      }
    }

    public static void Clear()
    {
      lock (Sync)
      {
        latestHands = Array.Empty<TrackedHand>();
      }
    }
  }
}
