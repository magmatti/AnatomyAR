using System.Collections.Generic;
using UnityEngine;

public class HandJointTracker : MonoBehaviour
{
    [SerializeField] private float minimumConfidence = 0.5f;
    [SerializeField] private float smoothingSpeed = 18f;

    public bool IsHandVisible { get; private set; }

    private readonly Dictionary<HandJointType, Vector3> smoothedPositions = new();
    private readonly HashSet<HandJointType> trackedJoints = new();

    public void UpdateHand(List<HandJointData> joints)
    {
        trackedJoints.Clear();

        foreach (HandJointData joint in joints)
        {
            if (joint.confidence < minimumConfidence)
            {
                continue;
            }

            if (!IsValidJointType(joint.jointType))
            {
                continue;
            }

            if (!smoothedPositions.ContainsKey(joint.jointType))
            {
                smoothedPositions[joint.jointType] = joint.position;
            }

            Vector3 previousPosition = smoothedPositions[joint.jointType];
            Vector3 newPosition = Vector3.Lerp(
                previousPosition,
                joint.position,
                Time.deltaTime * smoothingSpeed
            );

            smoothedPositions[joint.jointType] = newPosition;
            trackedJoints.Add(joint.jointType);
        }

        IsHandVisible = trackedJoints.Count > 0;
    }

    public void HideHand()
    {
        trackedJoints.Clear();
        IsHandVisible = false;
    }

    public bool TryGetSmoothedPosition(HandJointType jointType, out Vector3 position)
    {
        return smoothedPositions.TryGetValue(jointType, out position);
    }

    public bool IsJointTracked(HandJointType jointType)
    {
        return trackedJoints.Contains(jointType);
    }

    private static bool IsValidJointType(HandJointType jointType)
    {
        int jointIndex = (int)jointType;
        return jointIndex >= (int)HandJointType.Wrist && jointIndex <= (int)HandJointType.LittleTip;
    }
}
