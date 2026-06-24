using UnityEngine;

public class BodyRegionSelector : MonoBehaviour
{
    [SerializeField] private BodyJointTracker jointProvider;
    [SerializeField] private SkeletonRegionDisplayController displayController;
    [SerializeField] private Camera arCamera;
    [SerializeField] private float minimumScore = 0.55f;
    [SerializeField] private float switchDelay = 0.25f;
    [SerializeField] private float hideDelay = 0.4f;
    [SerializeField] private float minimumViewportSpan = 0.08f;
    [SerializeField] private float idealViewportSpan = 0.45f;
    [SerializeField] private float minimumTrackedWorldSpan = 0.18f;

    public BodyRegionType CurrentRegion { get; private set; }
    public bool HasCurrentRegion { get; private set; }

    private BodyRegionScorer regionScorer;
    private BodyRegionAnchorCalculator anchorCalculator;
    private BodyRegionType? pendingRegion;
    private float pendingStartedAt;
    private float lastReliableBodyAt;

    private void Awake()
    {
        if (jointProvider == null)
        {
            jointProvider = FindFirstObjectByType<BodyJointTracker>();
        }

        if (displayController == null)
        {
            displayController = FindFirstObjectByType<SkeletonRegionDisplayController>(FindObjectsInactive.Include);
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        regionScorer = new BodyRegionScorer(minimumViewportSpan, idealViewportSpan);
        anchorCalculator = new BodyRegionAnchorCalculator(minimumTrackedWorldSpan);
    }

    private void Update()
    {
        if (jointProvider == null || displayController == null || arCamera == null)
        {
            return;
        }

        if (!jointProvider.IsBodyVisible)
        {
            HideIfStale();
            return;
        }

        if (regionScorer.TryFindBestRegion(
                BodySkeletonData.SupportedRegionMappings,
                jointProvider,
                arCamera,
                out BodyRegionJointMapping bestMapping,
                out float bestScore) &&
            bestScore >= minimumScore)
        {
            lastReliableBodyAt = Time.time;

            if (ApplyStableRegion(bestMapping.Region))
            {
                UpdateRegionPlacement(bestMapping);
            }
        }
        else
        {
            HideIfStale();
        }
    }

    private bool ApplyStableRegion(BodyRegionType bestRegion)
    {
        if (HasCurrentRegion && CurrentRegion == bestRegion)
        {
            pendingRegion = null;
            return true;
        }

        if (pendingRegion != bestRegion)
        {
            pendingRegion = bestRegion;
            pendingStartedAt = Time.time;
            return false;
        }

        if (Time.time - pendingStartedAt >= switchDelay)
        {
            ShowRegion(bestRegion);
            pendingRegion = null;
            return true;
        }

        return false;
    }

    private void ShowRegion(BodyRegionType region)
    {
        CurrentRegion = region;
        HasCurrentRegion = true;
        lastReliableBodyAt = Time.time;
        displayController.ShowRegion(region);
    }

    private void HideIfStale()
    {
        if (HasCurrentRegion && Time.time - lastReliableBodyAt >= hideDelay)
        {
            HideCurrentRegion();
        }
    }

    private void HideCurrentRegion()
    {
        HasCurrentRegion = false;
        pendingRegion = null;

        if (displayController != null)
        {
            displayController.HideAll();
        }
    }

    private void UpdateRegionPlacement(BodyRegionJointMapping mapping)
    {
        if (anchorCalculator.TryGetAnchor(mapping, jointProvider, out Vector3 center, out float span))
        {
            displayController.AlignVisibleRegion(center, span, arCamera);
        }
    }
}
