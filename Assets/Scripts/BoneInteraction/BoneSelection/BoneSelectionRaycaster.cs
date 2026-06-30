using System;
using UnityEngine;

public sealed class BoneSelectionRaycaster
{
    private const float MinimumRayDistance = 20f;

    private readonly BoneTargetResolver targetResolver;
    private readonly BoneColliderService colliderService;

    public BoneSelectionRaycaster(
        BoneTargetResolver targetResolver,
        BoneColliderService colliderService)
    {
        this.targetResolver = targetResolver;
        this.colliderService = colliderService;
    }

    public bool TrySelect(
        Vector2 screenPosition,
        float maxRayDistance,
        LayerMask raycastMask,
        out BoneSelection selection)
    {
        selection = default;

        if (!PrepareTargets(out Camera camera))
        {
            return false;
        }

        if (!GetSortedHits(camera, screenPosition, maxRayDistance, raycastMask, out RaycastHit[] hits))
        {
            return false;
        }

        return ResolveSelection(hits, out selection);
    }

    private bool PrepareTargets(out Camera camera)
    {
        targetResolver.ResolveReferences();
        camera = targetResolver.Camera;

        if (camera == null)
        {
            return false;
        }

        colliderService.EnsureColliders(targetResolver.GetTargetRenderers());
        return true;
    }

    private static bool GetSortedHits(
        Camera camera,
        Vector2 screenPosition,
        float maxRayDistance,
        LayerMask raycastMask,
        out RaycastHit[] hits)
    {
        Ray ray = camera.ScreenPointToRay(screenPosition);
        hits = Physics.RaycastAll(
            ray,
            Mathf.Max(maxRayDistance, MinimumRayDistance),
            raycastMask,
            QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        return true;
    }

    private bool ResolveSelection(RaycastHit[] hits, out BoneSelection selection)
    {
        selection = default;

        foreach (RaycastHit hit in hits)
        {
            if (!targetResolver.TryResolveHit(hit.transform, out string label))
            {
                continue;
            }

            selection = new BoneSelection(hit.transform, label);
            return true;
        }

        return false;
    }
}
