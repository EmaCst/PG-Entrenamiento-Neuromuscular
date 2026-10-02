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
    public readonly struct TrackedLandmark
    {
      public readonly float x;
      public readonly float y;
      public readonly float z;

      public TrackedLandmark(float x, float y, float z)
      {
        this.x = x;
        this.y = y;
        this.z = z;
      }
    }

    public readonly struct TrackedHand
    {
      public readonly float x;
      public readonly float y;
      public readonly string handedness;
      public readonly TrackedLandmark[] landmarks;

      public TrackedHand(float x, float y, string handedness, TrackedLandmark[] landmarks)
      {
        this.x = x;
        this.y = y;
        this.handedness = handedness;
        this.landmarks = landmarks;
      }
    }

    private const int IndexFingerTip = 8;
    private static readonly object Sync = new object();
    private static TrackedHand[] latestHands = Array.Empty<TrackedHand>();
    private static bool sourceFlipHorizontally;
    private static bool sourceFlipVertically;

    public static bool SourceFlipHorizontally
    {
      get { lock (Sync) return sourceFlipHorizontally; }
    }

    public static bool SourceFlipVertically
    {
      get { lock (Sync) return sourceFlipVertically; }
    }

    public static void Publish(HandLandmarkerResult result, bool flipHorizontally, bool flipVertically)
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
        var trackedLandmarks = new TrackedLandmark[landmarks.Count];
        for (var landmarkIndex = 0; landmarkIndex < landmarks.Count; landmarkIndex++)
        {
          var landmark = landmarks[landmarkIndex];
          trackedLandmarks[landmarkIndex] = new TrackedLandmark(landmark.x, landmark.y, landmark.z);
        }

        var label = string.Empty;

        if (result.handedness != null &&
            i < result.handedness.Count &&
            result.handedness[i].categories != null &&
            result.handedness[i].categories.Count > 0)
        {
          label = result.handedness[i].categories[0].categoryName;
        }

        hands[i] = new TrackedHand(fingertip.x, fingertip.y, label, trackedLandmarks);
      }

      lock (Sync)
      {
        latestHands = hands;
        sourceFlipHorizontally = flipHorizontally;
        sourceFlipVertically = flipVertically;
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
        sourceFlipHorizontally = false;
        sourceFlipVertically = false;
      }
    }
  }
}
