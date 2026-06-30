using UnityEngine;

internal sealed class HandModelPlacementCalculator
{
    private const float MinimumDirectionMagnitude = 1e-6f;

    public bool TryCalculateTargetPlacement(
        HandModelJointPose jointPose,
        Vector3 positionOffset,
        Vector3 rotationOffsetEuler,
        float scaleMultiplier,
        bool scaleFromFingerTip,
        Camera arCamera,
        out HandModelPlacement placement)
    {
        placement = default;

        if (!TryCalculateBaseRotation(jointPose, out Quaternion baseRotation))
        {
            return false;
        }

        Vector3 palmCenter = CalculatePalmCenter(jointPose);
        Quaternion targetRotation = CalculateTargetRotation(baseRotation, rotationOffsetEuler);
        float handSize = CalculateHandSize(jointPose, scaleFromFingerTip);
        Vector3 targetScale = CalculateTargetScale(handSize, scaleMultiplier);
        Vector3 targetPosition = CalculateTargetPosition(
            palmCenter,
            baseRotation,
            positionOffset,
            handSize,
            arCamera
        );

        placement = new HandModelPlacement(targetPosition, targetRotation, targetScale);
        return true;
    }

    private static Vector3 CalculatePalmCenter(HandModelJointPose jointPose)
    {
        return (jointPose.Wrist +
                jointPose.IndexMCP +
                jointPose.MiddleMCP +
                jointPose.RingMCP +
                jointPose.LittleMCP) * 0.2f;
    }

    private static bool TryCalculateBaseRotation(
        HandModelJointPose jointPose,
        out Quaternion baseRotation)
    {
        baseRotation = Quaternion.identity;

        Vector3 handUp = jointPose.MiddleMCP - jointPose.Wrist;
        Vector3 acrossPalm = jointPose.LittleMCP - jointPose.IndexMCP;

        if (handUp.sqrMagnitude < MinimumDirectionMagnitude ||
            acrossPalm.sqrMagnitude < MinimumDirectionMagnitude)
        {
            return false;
        }

        handUp.Normalize();
        acrossPalm.Normalize();

        Vector3 handForward = Vector3.Cross(acrossPalm, handUp);

        if (handForward.sqrMagnitude < MinimumDirectionMagnitude)
        {
            return false;
        }

        handForward.Normalize();

        baseRotation = Quaternion.LookRotation(handForward, handUp);
        return true;
    }

    private static Quaternion CalculateTargetRotation(
        Quaternion baseRotation,
        Vector3 rotationOffsetEuler)
    {
        return baseRotation * Quaternion.Euler(rotationOffsetEuler);
    }

    private static float CalculateHandSize(
        HandModelJointPose jointPose,
        bool scaleFromFingerTip)
    {
        if (scaleFromFingerTip && jointPose.HasMiddleTip)
        {
            return Vector3.Distance(jointPose.Wrist, jointPose.MiddleTip);
        }

        return Vector3.Distance(jointPose.Wrist, jointPose.MiddleMCP);
    }

    private static Vector3 CalculateTargetScale(float handSize, float scaleMultiplier)
    {
        return Vector3.one * (handSize * scaleMultiplier);
    }

    private static Vector3 CalculateTargetPosition(
        Vector3 palmCenter,
        Quaternion baseRotation,
        Vector3 positionOffset,
        float handSize,
        Camera arCamera)
    {
        Vector3 inPlaneOffset = baseRotation * new Vector3(positionOffset.x, positionOffset.y, 0f);
        Vector3 depthOffset = CalculateDepthOffset(palmCenter, positionOffset.z, arCamera);

        return palmCenter + (inPlaneOffset + depthOffset) * handSize;
    }

    private static Vector3 CalculateDepthOffset(
        Vector3 palmCenter,
        float depthOffset,
        Camera arCamera)
    {
        if (arCamera == null || Mathf.Abs(depthOffset) <= MinimumDirectionMagnitude)
        {
            return Vector3.zero;
        }

        Vector3 toCamera = arCamera.transform.position - palmCenter;
        if (toCamera.sqrMagnitude <= MinimumDirectionMagnitude)
        {
            return Vector3.zero;
        }

        return toCamera.normalized * depthOffset;
    }
}
