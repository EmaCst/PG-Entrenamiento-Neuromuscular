using System.Collections;
using System.Collections.Generic;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class MediaPipeFootInteraction : MonoBehaviour
{
    public bool flipHorizontal;
    public bool swapFootLabels;
    public float minimumVisibility = 0.5f;
    public float maximumDistance = 100f;
    public RectTransform videoRect;

    private Camera interactionCamera;
    private readonly Vector3[] videoCorners = new Vector3[4];

    private void Awake()
    {
        interactionCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        foreach (var foot in PoseTrackingBridge.GetLatestFeet())
        {
            if (foot.visibility < minimumVisibility)
            {
                continue;
            }

            TryInteract(foot);
        }
    }

    private void TryInteract(PoseTrackingBridge.TrackedFoot foot)
    {
        float x = flipHorizontal ? 1f - foot.x : foot.x;
        float y = 1f - foot.y;

        if (x < 0f || x > 1f || y < 0f || y > 1f)
        {
            return;
        }

        var ray = CreateInteractionRay(x, y);
        var hits = Physics.RaycastAll(ray, maximumDistance);

        foreach (var hit in hits)
        {
            var target = hit.collider.GetComponentInParent<FootTargetController>();
            if (target == null)
            {
                continue;
            }

            var side = foot.side == PoseTrackingBridge.FootSide.Left
                ? RequiredFoot.Left
                : RequiredFoot.Right;

            if (swapFootLabels)
            {
                side = side == RequiredFoot.Left ? RequiredFoot.Right : RequiredFoot.Left;
            }

            target.Touch(side);
            return;
        }
    }

    private Ray CreateInteractionRay(float normalizedX, float normalizedY)
    {
        if (videoRect == null)
        {
            return interactionCamera.ViewportPointToRay(new Vector3(normalizedX, normalizedY, 0f));
        }

        videoRect.GetWorldCorners(videoCorners);

        var canvas = videoRect.GetComponentInParent<Canvas>();
        var canvasCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera
            : null;

        var bottomLeft = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[0]);
        var topRight = RectTransformUtility.WorldToScreenPoint(canvasCamera, videoCorners[2]);

        var screenPoint = new Vector2(
            Mathf.Lerp(bottomLeft.x, topRight.x, normalizedX),
            Mathf.Lerp(bottomLeft.y, topRight.y, normalizedY)
        );

        return interactionCamera.ScreenPointToRay(screenPoint);
    }
}

