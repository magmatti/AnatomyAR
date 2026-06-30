using UnityEngine;

internal sealed class BodyRegionAnchorCalculator
{
    private readonly float minimumTrackedWorldSpan;

    public BodyRegionAnchorCalculator(float minimumTrackedWorldSpan)
    {
        this.minimumTrackedWorldSpan = minimumTrackedWorldSpan;
    }

    public bool TryGetAnchor(
        BodyRegionJointMapping mapping,
        BodyJointTracker jointProvider,
        out Vector3 center,
        out float span)
    {
        center = Vector3.zero;
        span = 0f;

        if (!TryGetJointPositions(mapping, jointProvider, out Vector3[] positions))
        {
            return false;
        }

        center = CalculateCenter(positions);
        span = ClampSpan(CalculateSpan(positions));
        return true;
    }

    private static bool TryGetJointPositions(
        BodyRegionJointMapping mapping,
        BodyJointTracker jointProvider,
        out Vector3[] positions)
    {
        positions = null;

        if (mapping == null || mapping.Joints == null || mapping.Joints.Length == 0 || jointProvider == null)
        {
            return false;
        }

        positions = new Vector3[mapping.Joints.Length];

        for (int i = 0; i < mapping.Joints.Length; i++)
        {
            if (!jointProvider.TryGetSmoothedPosition(mapping.Joints[i], out Vector3 worldPosition))
            {
                return false;
            }

            positions[i] = worldPosition;
        }

        return true;
    }

    private static Vector3 CalculateCenter(Vector3[] positions)
    {
        Vector3 center = Vector3.zero;

        foreach (Vector3 position in positions)
        {
            center += position;
        }

        return center / positions.Length;
    }

    private static float CalculateSpan(Vector3[] positions)
    {
        float span = 0f;

        for (int i = 0; i < positions.Length; i++)
        {
            for (int j = i + 1; j < positions.Length; j++)
            {
                span = Mathf.Max(span, Vector3.Distance(positions[i], positions[j]));
            }
        }

        return span;
    }

    private float ClampSpan(float span)
    {
        if (span <= 1e-4f)
        {
            return minimumTrackedWorldSpan;
        }

        return Mathf.Max(span, minimumTrackedWorldSpan);
    }
}
