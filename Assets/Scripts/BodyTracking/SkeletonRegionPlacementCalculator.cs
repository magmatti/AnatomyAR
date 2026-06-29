using UnityEngine;

internal sealed class SkeletonRegionPlacementCalculator
{
    private const float MinimumSpan = 1e-4f;
    private const float MinimumDirectionMagnitude = 1e-6f;

    public bool TryCalculateTargetPlacement(
        Bounds localBounds,
        Vector3 targetCenter,
        float targetWorldSpan,
        float trackedScaleMultiplier,
        float minimumScale,
        float maximumScale,
        bool faceCameraWhenTracked,
        Vector3 modelRotationOffsetEuler,
        Vector3 cameraRelativeOffset,
        Camera arCamera,
        out SkeletonRegionPlacement placement)
    {
        placement = default;
        float localSpan = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);

        if (targetWorldSpan <= MinimumSpan || localSpan <= MinimumSpan) return false;

        // match skeleton size to the tracked body region
        float targetUniformScale = CalculateTargetUniformScale(
            targetWorldSpan,
            trackedScaleMultiplier,
            localSpan,
            minimumScale,
            maximumScale
        );

        // build the final target transform for the skeleton root
        Vector3 targetScale = Vector3.one * targetUniformScale;
        Quaternion targetRotation = GetTargetRotation(
            targetCenter,
            arCamera,
            faceCameraWhenTracked,
            modelRotationOffsetEuler
        );
        Vector3 offset = GetCameraRelativeOffset(arCamera, cameraRelativeOffset);
        Vector3 targetPosition = CalculateTargetPosition(
            targetCenter,
            offset,
            targetRotation,
            localBounds.center,
            targetUniformScale
        );

        placement = new SkeletonRegionPlacement(targetPosition, targetRotation, targetScale);
        return true;
    }

    private static bool TryGetHorizontalDirectionToCamera(
        Vector3 targetCenter,
        Camera arCamera,
        out Vector3 direction)
    {
        direction = arCamera.transform.position - targetCenter;
        direction.y = 0f;

        if (direction.sqrMagnitude <= MinimumDirectionMagnitude)
        {
            direction = -arCamera.transform.forward;
            direction.y = 0f;
        }

        if (direction.sqrMagnitude <= MinimumDirectionMagnitude) return false;

        direction.Normalize();
        return true;
    }

    private static Quaternion GetTargetRotation(
        Vector3 targetCenter,
        Camera arCamera,
        bool faceCameraWhenTracked,
        Vector3 modelRotationOffsetEuler)
    {
        Quaternion offsetRotation = Quaternion.Euler(modelRotationOffsetEuler);

        if (!faceCameraWhenTracked || arCamera == null)
        {
            return offsetRotation;
        }

        // face horizontally toward the camera then apply the model offset
        if (!TryGetHorizontalDirectionToCamera(targetCenter, arCamera, out Vector3 direction))
        {
            return offsetRotation;
        }

        return Quaternion.LookRotation(direction, Vector3.up) * offsetRotation;
    }

    private static float CalculateTargetUniformScale(
        float targetWorldSpan,
        float trackedScaleMultiplier,
        float localSpan,
        float minimumScale,
        float maximumScale)
    {
        return Mathf.Clamp(
            targetWorldSpan * trackedScaleMultiplier / localSpan,
            minimumScale,
            maximumScale
        );
    }

    private static Vector3 CalculateTargetPosition(
        Vector3 targetCenter,
        Vector3 offset,
        Quaternion targetRotation,
        Vector3 localBoundsCenter,
        float targetUniformScale)
    {
        return targetCenter + offset - targetRotation * (localBoundsCenter * targetUniformScale);
    }

    private static Vector3 GetCameraRelativeOffset(Camera arCamera, Vector3 cameraRelativeOffset)
    {
        if (arCamera == null) return cameraRelativeOffset;

        Transform cameraTransform = arCamera.transform;

        return cameraTransform.right * cameraRelativeOffset.x +
               cameraTransform.up * cameraRelativeOffset.y +
               cameraTransform.forward * cameraRelativeOffset.z;
    }
}
