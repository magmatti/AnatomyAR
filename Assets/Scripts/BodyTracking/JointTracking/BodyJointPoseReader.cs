using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

public static class BodyJointPoseReader
{
    public static bool TryGetTrackedWorldPose(
        Transform bodyTransform,
        NativeArray<XRHumanBodyJoint> joints,
        BodyJointIndexMapping mapping,
        out Pose worldPose)
    {
        worldPose = default;

        if (!IsValidMapping(mapping, joints.Length))
        {
            return false;
        }

        XRHumanBodyJoint joint = joints[mapping.arKitJointIndex];

        if (!joint.tracked)
        {
            return false;
        }

        worldPose = new Pose(
            bodyTransform.TransformPoint(joint.anchorPose.position),
            bodyTransform.rotation * joint.anchorPose.rotation
        );

        return true;
    }

    private static bool IsValidMapping(BodyJointIndexMapping mapping, int jointCount)
    {
        return mapping != null
            && mapping.arKitJointIndex >= 0
            && mapping.arKitJointIndex < jointCount;
    }
}
