using UnityEngine;

internal readonly struct HandModelJointPose
{
    public HandModelJointPose(
        Vector3 wrist,
        Vector3 indexMCP,
        Vector3 middleMCP,
        Vector3 ringMCP,
        Vector3 littleMCP,
        bool hasMiddleTip,
        Vector3 middleTip)
    {
        Wrist = wrist;
        IndexMCP = indexMCP;
        MiddleMCP = middleMCP;
        RingMCP = ringMCP;
        LittleMCP = littleMCP;
        HasMiddleTip = hasMiddleTip;
        MiddleTip = middleTip;
    }

    public Vector3 Wrist { get; }
    public Vector3 IndexMCP { get; }
    public Vector3 MiddleMCP { get; }
    public Vector3 RingMCP { get; }
    public Vector3 LittleMCP { get; }
    public bool HasMiddleTip { get; }
    public Vector3 MiddleTip { get; }
}
