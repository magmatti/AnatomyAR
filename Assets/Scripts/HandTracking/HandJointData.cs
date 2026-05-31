using UnityEngine;

public struct HandJointData
{
    public HandJointType jointType;
    public Vector3 position;
    public float confidence;

    public HandJointData(HandJointType jointType, Vector3 position, float confidence)
    {
        this.jointType = jointType;
        this.position = position;
        this.confidence = confidence;
    }
}