using System.Collections.Generic;
using UnityEngine;

internal sealed class SkeletonVisibleBoundsCalculator
{
    private readonly Transform root;

    public SkeletonVisibleBoundsCalculator(Transform root)
    {
        this.root = root;
    }

    public bool TryGetVisibleLocalBounds(
        IEnumerable<Renderer> renderers,
        ICollection<Transform> visibleParts,
        out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        if (root == null || renderers == null || visibleParts == null || visibleParts.Count == 0)
        {
            return false;
        }

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !visibleParts.Contains(renderer.transform))
            {
                continue;
            }

            EncapsulateRendererBounds(renderer.bounds, ref localBounds, ref hasBounds);
        }

        return hasBounds;
    }

    private void EncapsulateRendererBounds(
        Bounds rendererBounds,
        ref Bounds localBounds,
        ref bool hasBounds)
    {
        Vector3 min = rendererBounds.min;
        Vector3 max = rendererBounds.max;

        EncapsulateWorldPoint(new Vector3(min.x, min.y, min.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(min.x, min.y, max.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(min.x, max.y, min.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(min.x, max.y, max.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(max.x, min.y, min.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(max.x, min.y, max.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(max.x, max.y, min.z), ref localBounds, ref hasBounds);
        EncapsulateWorldPoint(new Vector3(max.x, max.y, max.z), ref localBounds, ref hasBounds);
    }

    private void EncapsulateWorldPoint(
        Vector3 worldPoint,
        ref Bounds localBounds,
        ref bool hasBounds)
    {
        Vector3 localPoint = root.InverseTransformPoint(worldPoint);

        if (!hasBounds)
        {
            localBounds = new Bounds(localPoint, Vector3.zero);
            hasBounds = true;
            return;
        }

        localBounds.Encapsulate(localPoint);
    }
}
