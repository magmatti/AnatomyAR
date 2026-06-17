using UnityEngine;

public readonly struct BoneSelection
{
    public BoneSelection(Transform boneTransform, string label)
    {
        BoneTransform = boneTransform;
        Label = label ?? string.Empty;
    }

    public Transform BoneTransform { get; }
    public string Label { get; }
}
