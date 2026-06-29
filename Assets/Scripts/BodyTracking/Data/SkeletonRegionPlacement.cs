using UnityEngine;

internal readonly struct SkeletonRegionPlacement
{
    public SkeletonRegionPlacement(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }
}
