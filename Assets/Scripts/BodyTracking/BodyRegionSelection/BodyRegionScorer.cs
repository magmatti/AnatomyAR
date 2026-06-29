using System.Collections.Generic;
using UnityEngine;

internal sealed class BodyRegionScorer
{
    private const float CenterScoreMaxDistance = 0.65f;
    private const float CenterScoreWeight = 0.75f;
    private const float SizeScoreWeight = 0.25f;
    private static readonly Vector2 ViewportCenter = new(0.5f, 0.5f);

    private readonly float minimumViewportSpan;
    private readonly float idealViewportSpan;

    public BodyRegionScorer(float minimumViewportSpan, float idealViewportSpan)
    {
        this.minimumViewportSpan = minimumViewportSpan;
        this.idealViewportSpan = idealViewportSpan;
    }

    public bool TryFindBestRegion(
        IEnumerable<BodyRegionJointMapping> mappings,
        BodyJointTracker jointProvider,
        Camera arCamera,
        out BodyRegionJointMapping bestMapping,
        out float bestScore)
    {
        bestMapping = null;
        bestScore = float.MinValue;

        if (mappings == null || jointProvider == null || arCamera == null)
        {
            return false;
        }

        foreach (BodyRegionJointMapping mapping in mappings)
        {
            if (!TryScoreMapping(mapping, jointProvider, arCamera, out float score))
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMapping = mapping;
            }
        }

        return bestMapping != null;
    }

    private bool TryScoreMapping(
        BodyRegionJointMapping mapping,
        BodyJointTracker jointProvider,
        Camera arCamera,
        out float score)
    {
        score = 0f;

        if (!TryGetViewportBounds(mapping, jointProvider, arCamera, out Vector2 center, out Vector2 size))
        {
            return false;
        }

        score = CalculateScore(center, size);
        return true;
    }

    private bool TryGetViewportBounds(
        BodyRegionJointMapping mapping,
        BodyJointTracker jointProvider,
        Camera arCamera,
        out Vector2 center,
        out Vector2 size)
    {
        center = Vector2.zero;
        size = Vector2.zero;

        if (mapping == null || mapping.Joints == null || mapping.Joints.Length == 0)
        {
            return false;
        }

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);

        foreach (BodyJointType jointType in mapping.Joints)
        {
            if (!TryProjectJoint(jointType, jointProvider, arCamera, out Vector2 viewportPosition))
            {
                return false;
            }

            center += viewportPosition;
            min = Vector2.Min(min, viewportPosition);
            max = Vector2.Max(max, viewportPosition);
        }

        center /= mapping.Joints.Length;
        size = max - min;
        return true;
    }

    private static bool TryProjectJoint(
        BodyJointType jointType,
        BodyJointTracker jointProvider,
        Camera arCamera,
        out Vector2 viewportPosition)
    {
        viewportPosition = Vector2.zero;

        if (!jointProvider.TryGetSmoothedPosition(jointType, out Vector3 worldPosition))
        {
            return false;
        }

        Vector3 projectedPosition = arCamera.WorldToViewportPoint(worldPosition);

        if (projectedPosition.z <= 0f)
        {
            return false;
        }

        viewportPosition = new Vector2(projectedPosition.x, projectedPosition.y);
        return true;
    }

    private float CalculateScore(Vector2 center, Vector2 size)
    {
        float centerScore = CalculateCenterScore(center);
        float sizeScore = CalculateSizeScore(size);
        return centerScore * CenterScoreWeight + sizeScore * SizeScoreWeight;
    }

    private static float CalculateCenterScore(Vector2 center)
    {
        float distanceFromCenter = Vector2.Distance(center, ViewportCenter);
        return Mathf.Clamp01(1f - distanceFromCenter / CenterScoreMaxDistance);
    }

    private float CalculateSizeScore(Vector2 size)
    {
        float viewportSpan = Mathf.Max(size.x, size.y);
        return Mathf.InverseLerp(minimumViewportSpan, idealViewportSpan, viewportSpan);
    }
}
