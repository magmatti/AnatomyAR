using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BodyRegionSelector : MonoBehaviour
{
    private class BodyRegionRule
    {
        public BodyRegionType region;
        public BodyJointType[] joints;

        public BodyRegionRule(BodyRegionType region, params BodyJointType[] joints)
        {
            this.region = region;
            this.joints = joints;
        }
    }

    [Header("References")]
    [SerializeField] private BodyJointVisualizer jointProvider;
    [SerializeField] private SkeletonRegionDisplayController displayController;
    [SerializeField] private Camera arCamera;

    [Header("Selection")]
    [SerializeField] private float minimumScore = 0.55f;
    [SerializeField] private float switchDelay = 0.25f;
    [SerializeField] private float hideDelay = 0.4f;
    [SerializeField] private float minimumViewportSpan = 0.08f;
    [SerializeField] private float idealViewportSpan = 0.45f;
    [SerializeField] private float minimumTrackedWorldSpan = 0.18f;

    [Header("Editor Debug")]
    [SerializeField] private bool enableKeyboardDebug = true;

    public BodyRegionType CurrentRegion { get; private set; }
    public bool HasCurrentRegion { get; private set; }

    private readonly List<BodyRegionRule> rules = new();
    private readonly BodyRegionType[] debugRegions =
    {
        BodyRegionType.Torso,
        BodyRegionType.LeftUpperArm,
        BodyRegionType.LeftForearm,
        BodyRegionType.LeftHand,
        BodyRegionType.LeftThigh,
        BodyRegionType.LeftLowerLeg,
        BodyRegionType.LeftFoot,
        BodyRegionType.RightUpperArm,
        BodyRegionType.RightForearm,
        BodyRegionType.RightHand,
        BodyRegionType.RightThigh,
        BodyRegionType.RightLowerLeg,
        BodyRegionType.RightFoot
    };
    private BodyRegionType? pendingRegion;
    private float pendingStartedAt;
    private float lastReliableBodyAt;

    private void Awake()
    {
        if (jointProvider == null)
        {
            jointProvider = FindFirstObjectByType<BodyJointVisualizer>();
        }

        if (displayController == null)
        {
            displayController = FindFirstObjectByType<SkeletonRegionDisplayController>(FindObjectsInactive.Include);
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        BuildRules();
    }

    private void Update()
    {
        if (HandleKeyboardDebug())
        {
            return;
        }

        if (jointProvider == null || displayController == null || arCamera == null)
        {
            return;
        }

        if (!jointProvider.IsBodyVisible)
        {
            HideIfStale();
            return;
        }

        if (TryFindBestRegion(out BodyRegionType bestRegion, out float bestScore) && bestScore >= minimumScore)
        {
            lastReliableBodyAt = Time.time;
            ApplyStableRegion(bestRegion);
            UpdateRegionPlacement(bestRegion);
        }
        else
        {
            HideIfStale();
        }
    }

    private bool HandleKeyboardDebug()
    {
        if (!enableKeyboardDebug || Keyboard.current == null || displayController == null)
        {
            return false;
        }

        BodyRegionType? debugRegion = null;

        if (Keyboard.current.digit1Key.wasPressedThisFrame) debugRegion = BodyRegionType.Torso;
        if (Keyboard.current.digit2Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftUpperArm;
        if (Keyboard.current.digit3Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftForearm;
        if (Keyboard.current.digit4Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftHand;
        if (Keyboard.current.digit5Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftThigh;
        if (Keyboard.current.digit6Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftLowerLeg;
        if (Keyboard.current.digit7Key.wasPressedThisFrame) debugRegion = BodyRegionType.LeftFoot;
        if (Keyboard.current.digit8Key.wasPressedThisFrame) debugRegion = BodyRegionType.RightUpperArm;
        if (Keyboard.current.digit9Key.wasPressedThisFrame) debugRegion = BodyRegionType.RightForearm;
        if (Keyboard.current.digit0Key.wasPressedThisFrame) debugRegion = BodyRegionType.RightHand;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HideCurrentRegion();
            return true;
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            ShowRegion(GetNextDebugRegion(1));
            return true;
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            ShowRegion(GetNextDebugRegion(-1));
            return true;
        }

        if (!debugRegion.HasValue)
        {
            return false;
        }

        ShowRegion(debugRegion.Value);
        return true;
    }

    private BodyRegionType GetNextDebugRegion(int direction)
    {
        if (!HasCurrentRegion)
        {
            return BodyRegionType.Torso;
        }

        int currentIndex = 0;

        for (int i = 0; i < debugRegions.Length; i++)
        {
            if (debugRegions[i] == CurrentRegion)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex = (currentIndex + direction + debugRegions.Length) % debugRegions.Length;
        return debugRegions[nextIndex];
    }

    private bool TryFindBestRegion(out BodyRegionType bestRegion, out float bestScore)
    {
        bestRegion = default;
        bestScore = float.MinValue;

        foreach (BodyRegionRule rule in rules)
        {
            if (!TryScoreRule(rule, out float score))
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestRegion = rule.region;
            }
        }

        return bestScore > float.MinValue;
    }

    private bool TryScoreRule(BodyRegionRule rule, out float score)
    {
        score = 0f;

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        Vector2 center = Vector2.zero;

        foreach (BodyJointType jointType in rule.joints)
        {
            if (!jointProvider.TryGetSmoothedPosition(jointType, out Vector3 worldPosition))
            {
                return false;
            }

            Vector3 viewportPosition = arCamera.WorldToViewportPoint(worldPosition);

            if (viewportPosition.z <= 0f)
            {
                return false;
            }

            Vector2 viewport2D = new(viewportPosition.x, viewportPosition.y);
            center += viewport2D;
            min = Vector2.Min(min, viewport2D);
            max = Vector2.Max(max, viewport2D);
        }

        center /= rule.joints.Length;

        float distanceFromCenter = Vector2.Distance(center, new Vector2(0.5f, 0.5f));
        float centerScore = Mathf.Clamp01(1f - distanceFromCenter / 0.65f);

        Vector2 size = max - min;
        float viewportSpan = Mathf.Max(size.x, size.y);
        float sizeScore = Mathf.InverseLerp(minimumViewportSpan, idealViewportSpan, viewportSpan);

        score = centerScore * 0.75f + sizeScore * 0.25f;
        return true;
    }

    private void ApplyStableRegion(BodyRegionType bestRegion)
    {
        if (HasCurrentRegion && CurrentRegion == bestRegion)
        {
            pendingRegion = null;
            return;
        }

        if (pendingRegion != bestRegion)
        {
            pendingRegion = bestRegion;
            pendingStartedAt = Time.time;
            return;
        }

        if (Time.time - pendingStartedAt >= switchDelay)
        {
            ShowRegion(bestRegion);
            pendingRegion = null;
        }
    }

    private void ShowRegion(BodyRegionType region)
    {
        CurrentRegion = region;
        HasCurrentRegion = true;
        lastReliableBodyAt = Time.time;
        displayController.ShowRegion(region);
        UpdateRegionPlacement(region);
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

    private void BuildRules()
    {
        rules.Clear();

        rules.Add(new BodyRegionRule(BodyRegionType.Torso,
            BodyJointType.LeftShoulder, BodyJointType.RightShoulder, BodyJointType.LeftHip, BodyJointType.RightHip));

        rules.Add(new BodyRegionRule(BodyRegionType.LeftUpperArm, BodyJointType.LeftShoulder, BodyJointType.LeftElbow));
        rules.Add(new BodyRegionRule(BodyRegionType.RightUpperArm, BodyJointType.RightShoulder, BodyJointType.RightElbow));
        rules.Add(new BodyRegionRule(BodyRegionType.LeftForearm, BodyJointType.LeftElbow, BodyJointType.LeftWrist));
        rules.Add(new BodyRegionRule(BodyRegionType.RightForearm, BodyJointType.RightElbow, BodyJointType.RightWrist));
        rules.Add(new BodyRegionRule(BodyRegionType.LeftHand, BodyJointType.LeftWrist));
        rules.Add(new BodyRegionRule(BodyRegionType.RightHand, BodyJointType.RightWrist));

        rules.Add(new BodyRegionRule(BodyRegionType.LeftThigh, BodyJointType.LeftHip, BodyJointType.LeftKnee));
        rules.Add(new BodyRegionRule(BodyRegionType.RightThigh, BodyJointType.RightHip, BodyJointType.RightKnee));
        rules.Add(new BodyRegionRule(BodyRegionType.LeftLowerLeg, BodyJointType.LeftKnee, BodyJointType.LeftAnkle));
        rules.Add(new BodyRegionRule(BodyRegionType.RightLowerLeg, BodyJointType.RightKnee, BodyJointType.RightAnkle));
        rules.Add(new BodyRegionRule(BodyRegionType.LeftFoot, BodyJointType.LeftAnkle));
        rules.Add(new BodyRegionRule(BodyRegionType.RightFoot, BodyJointType.RightAnkle));
    }

    private void UpdateRegionPlacement(BodyRegionType region)
    {
        if (!HasCurrentRegion || CurrentRegion != region)
        {
            return;
        }

        if (TryGetRegionWorldAnchor(region, out Vector3 center, out float span))
        {
            displayController.AlignVisibleRegion(center, span, arCamera);
        }
    }

    private bool TryGetRegionWorldAnchor(BodyRegionType region, out Vector3 center, out float span)
    {
        center = Vector3.zero;
        span = 0f;

        BodyRegionRule rule = FindRule(region);

        if (rule == null || rule.joints == null || rule.joints.Length == 0)
        {
            return false;
        }

        int trackedCount = 0;
        Vector3[] positions = new Vector3[rule.joints.Length];

        for (int i = 0; i < rule.joints.Length; i++)
        {
            if (!jointProvider.TryGetSmoothedPosition(rule.joints[i], out Vector3 worldPosition))
            {
                return false;
            }

            positions[i] = worldPosition;
            center += worldPosition;
            trackedCount++;
        }

        if (trackedCount == 0)
        {
            return false;
        }

        center /= trackedCount;

        for (int i = 0; i < trackedCount; i++)
        {
            for (int j = i + 1; j < trackedCount; j++)
            {
                span = Mathf.Max(span, Vector3.Distance(positions[i], positions[j]));
            }
        }

        if (span <= 1e-4f)
        {
            span = minimumTrackedWorldSpan;
        }
        else
        {
            span = Mathf.Max(span, minimumTrackedWorldSpan);
        }

        return true;
    }

    private BodyRegionRule FindRule(BodyRegionType region)
    {
        foreach (BodyRegionRule rule in rules)
        {
            if (rule.region == region)
            {
                return rule;
            }
        }

        return null;
    }
}
