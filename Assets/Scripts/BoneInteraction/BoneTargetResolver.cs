using System.Collections.Generic;
using UnityEngine;

public sealed class BoneTargetResolver
{
    private readonly HandBoneNameResolver handNameResolver;
    private readonly SkeletonBoneNameResolver skeletonNameResolver = new();

    private Camera camera;
    private SkeletonModelController skeletonController;
    private Transform modelRoot;

    public BoneTargetResolver(
        Camera camera,
        SkeletonModelController skeletonController,
        Transform modelRoot,
        HandBoneNameResolver handNameResolver)
    {
        this.camera = camera;
        this.skeletonController = skeletonController;
        this.modelRoot = modelRoot;
        this.handNameResolver = handNameResolver;
    }

    public Camera Camera => camera;
    public Transform ModelRoot => modelRoot;

    public void ResolveReferences()
    {
        if (camera == null) camera = Camera.main;

        if (skeletonController == null)
        {
            skeletonController = Object
                .FindFirstObjectByType<SkeletonModelController>(FindObjectsInactive.Include);
        }

        if (modelRoot == null && skeletonController == null)
        {
            GameObject fallbackRoot = GameObject.Find("SkeletonModelRoot");
            if (fallbackRoot != null)
            {
                modelRoot = fallbackRoot.transform;
            }
        }
    }

    public IEnumerable<Renderer> GetTargetRenderers()
    {
        if (skeletonController != null) return skeletonController.AllRenderers;

        return modelRoot == null
            ? System.Array.Empty<Renderer>()
            : modelRoot.GetComponentsInChildren<Renderer>(true);
    }

    public bool TryResolveHit(Transform candidate, out string label)
    {
        label = string.Empty;

        if (candidate == null) return false;
        
        if (skeletonController != null)
        {
            if (!skeletonController.TryGetVisibleSkeletonPart(candidate, out Transform skeletonPart))
            {
                return false;
            }

            string cleanName = skeletonNameResolver.GetBoneName(skeletonPart);
            label = string.IsNullOrWhiteSpace(cleanName)
                ? skeletonNameResolver.GetBoneName(candidate)
                : cleanName;
            return true;
        }

        if (!IsVisibleModelPart(candidate, modelRoot))
        {
            return false;
        }

        label = handNameResolver.GetBoneName(candidate, modelRoot);
        return true;
    }

    private static bool IsVisibleModelPart(Transform candidate, Transform modelRoot)
    {
        if (modelRoot == null || candidate == null || !candidate.IsChildOf(modelRoot))
        {
            return false;
        }

        Renderer renderer = candidate.GetComponent<Renderer>();
        return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
    }
}
