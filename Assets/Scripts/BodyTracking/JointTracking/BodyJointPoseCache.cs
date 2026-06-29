using System.Collections.Generic;
using UnityEngine;

public sealed class BodyJointPoseCache
{
    private readonly Dictionary<BodyJointType, BodyJointState> jointStates = new();
    private readonly HashSet<BodyJointType> trackedJoints = new();
    private readonly List<BodyJointType> jointsToRemove = new();

    public bool IsBodyVisible => trackedJoints.Count > 0;

    public bool TryGetSmoothedPosition(BodyJointType jointType, out Vector3 position)
    {
        if (IsJointTracked(jointType) && jointStates.TryGetValue(jointType, out BodyJointState state))
        {
            position = state.SmoothedPosition;
            return true;
        }

        position = default;
        return false;
    }

    public bool TryGetTrackedRotation(BodyJointType jointType, out Quaternion rotation)
    {
        if (IsJointTracked(jointType) && jointStates.TryGetValue(jointType, out BodyJointState state))
        {
            rotation = state.Rotation;
            return true;
        }

        rotation = default;
        return false;
    }

    public bool IsJointTracked(BodyJointType jointType)
    {
        return trackedJoints.Contains(jointType);
    }

    public void BeginFrame()
    {
        trackedJoints.Clear();
    }

    public void TrackJoint(BodyJointType jointType, Pose worldPose, float smoothingSpeed, float deltaTime)
    {
        Vector3 smoothedPosition = CalculateSmoothedPosition(
            jointType,
            worldPose.position,
            smoothingSpeed,
            deltaTime
        );

        jointStates[jointType] = new BodyJointState(smoothedPosition, worldPose.rotation);
        trackedJoints.Add(jointType);
    }

    public void EndFrame()
    {
        jointsToRemove.Clear();

        foreach (BodyJointType jointType in jointStates.Keys)
        {
            if (!trackedJoints.Contains(jointType))
            {
                jointsToRemove.Add(jointType);
            }
        }

        foreach (BodyJointType jointType in jointsToRemove)
        {
            jointStates.Remove(jointType);
        }
    }

    public void Clear()
    {
        trackedJoints.Clear();
        jointStates.Clear();
    }

    private Vector3 CalculateSmoothedPosition(
        BodyJointType jointType,
        Vector3 worldPosition,
        float smoothingSpeed,
        float deltaTime)
    {
        if (!jointStates.TryGetValue(jointType, out BodyJointState previousState))
        {
            return worldPosition;
        }

        return Vector3.Lerp(
            previousState.SmoothedPosition,
            worldPosition,
            deltaTime * smoothingSpeed
        );
    }

    private readonly struct BodyJointState
    {
        public BodyJointState(Vector3 smoothedPosition, Quaternion rotation)
        {
            SmoothedPosition = smoothedPosition;
            Rotation = rotation;
        }

        public Vector3 SmoothedPosition { get; }
        public Quaternion Rotation { get; }
    }
}
