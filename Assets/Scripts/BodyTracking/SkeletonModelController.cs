using System.Collections.Generic;
using UnityEngine;

public class SkeletonModelController : MonoBehaviour
{
    [SerializeField] private Transform skeletonRoot;
    [SerializeField] private string leftRootName = "left_skeleton";
    [SerializeField] private string rightRootName = "right_skeleton";

    [SerializeField] private bool hideOnStart = true;

    // visibility
    [SerializeField] private bool modelViewEnabled = true;

    // tracked placement
    [SerializeField] private bool followTrackedRegion = true;
    [SerializeField] private float trackedScaleMultiplier = 1f;
    [SerializeField] private float minimumScale = 0.05f;
    [SerializeField] private float maximumScale = 5f;
    [SerializeField] private float positionSmoothing = 18f;
    [SerializeField] private float rotationSmoothing = 14f;
    [SerializeField] private float scaleSmoothing = 14f;
    [SerializeField] private bool faceCameraWhenTracked = true;
    
    [SerializeField] private Vector3 modelRotationOffsetEuler = Vector3.zero;
    [SerializeField] private Vector3 cameraRelativeOffset = Vector3.zero;

    private readonly List<Renderer> allRenderers = new();
    private readonly HashSet<Transform> visibleParts = new();
    private SkeletonRegionMatcher regionMatcher;
    private SkeletonVisibleBoundsCalculator boundsCalculator;
    private SkeletonRegionPlacementCalculator placementCalculator;
    private SkeletonRegionPoseSmoother poseSmoother;

    public BodyRegionType? CurrentRegion { get; private set; }

    public IReadOnlyList<Renderer> AllRenderers => allRenderers;
    public bool ModelViewEnabled => modelViewEnabled;

    private void Awake()
    {
        if (skeletonRoot == null) skeletonRoot = transform;

        Transform leftRoot = FindChildByName(skeletonRoot, leftRootName);
        Transform rightRoot = FindChildByName(skeletonRoot, rightRootName);

        regionMatcher = new SkeletonRegionMatcher(
            BodySkeletonData.CreateDefaultRegionDefinitions(), leftRoot, rightRoot);
        boundsCalculator = new SkeletonVisibleBoundsCalculator(skeletonRoot);
        placementCalculator = new SkeletonRegionPlacementCalculator();
        poseSmoother = new SkeletonRegionPoseSmoother();
        CollectRenderers();

        if (hideOnStart) HideAll();
    }

    public void SetModelViewEnabled(bool isEnabled)
    {
        if (modelViewEnabled == isEnabled) return;

        modelViewEnabled = isEnabled;

        if (!modelViewEnabled)
        {
            SetAllRenderersEnabled(false);
            return;
        }

        if (CurrentRegion.HasValue) ShowRegion(CurrentRegion.Value);
    }

    public void ShowRegion(BodyRegionType region)
    {
        if (CurrentRegion != region) poseSmoother.Reset();

        CurrentRegion = region;
        visibleParts.Clear();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            bool visible = regionMatcher.MatchesRegion(renderer.transform, region);
            renderer.enabled = modelViewEnabled && visible;

            if (visible) visibleParts.Add(renderer.transform);
        }
    }

    public void HideAll()
    {
        CurrentRegion = null;
        poseSmoother.Reset();
        visibleParts.Clear();

        SetAllRenderersEnabled(false);
    }

    public void AlignVisibleRegion(Vector3 targetCenter, float targetWorldSpan, Camera arCamera)
    {
        if (!followTrackedRegion || visibleParts.Count == 0) return;

        if (!boundsCalculator.TryGetVisibleLocalBounds(
            allRenderers, visibleParts, out Bounds localBounds)) return;

        if (!placementCalculator.TryCalculateTargetPlacement(
                localBounds,
                targetCenter,
                targetWorldSpan,
                trackedScaleMultiplier,
                minimumScale,
                maximumScale,
                faceCameraWhenTracked,
                modelRotationOffsetEuler,
                cameraRelativeOffset,
                arCamera,
                out SkeletonRegionPlacement targetPlacement)) return;

        poseSmoother.Apply(
            skeletonRoot,
            targetPlacement,
            positionSmoothing,
            rotationSmoothing,
            scaleSmoothing
        );
    }

    public bool TryGetVisibleSkeletonPart(Transform candidate, out Transform skeletonPart)
    {
        skeletonPart = null;

        if (!modelViewEnabled) return false;

        while (candidate != null && candidate != skeletonRoot)
        {
            if (visibleParts.Contains(candidate))
            {
                skeletonPart = candidate;
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }

    private void SetAllRenderersEnabled(bool isEnabled)
    {
        foreach (Renderer renderer in allRenderers)
        {
            if (renderer != null) renderer.enabled = isEnabled;
        }
    }

    private void CollectRenderers()
    {
        allRenderers.Clear();
        skeletonRoot.GetComponentsInChildren(true, allRenderers);
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName)) return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }

        return null;
    }
}
