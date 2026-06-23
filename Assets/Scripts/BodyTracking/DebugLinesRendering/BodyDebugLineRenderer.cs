using System.Collections.Generic;
using UnityEngine;

public sealed class BodyDebugLineRenderer
{
    private readonly List<LineRenderer> debugLines = new();

    public void UpdateConnectionLines(
        BodyJointTracker tracker,
        Transform parent,
        float lineWidth,
        Material lineMaterial,
        Color lineColor)
    {
        EnsureConnectionLineRenderers(parent, lineWidth, lineMaterial, lineColor);

        int lineIndex = 0;

        foreach (BodyJointType[] connection in BodySkeletonData.BodyConnections)
        {
            for (int i = 0; i < connection.Length - 1; i++)
            {
                UpdateConnectionSegment(
                    debugLines[lineIndex++],
                    tracker,
                    connection[i],
                    connection[i + 1],
                    lineWidth
                );
            }
        }
    }

    public void HideLines()
    {
        foreach (LineRenderer line in debugLines)
        {
            line.enabled = false;
        }
    }

    private void EnsureConnectionLineRenderers(
        Transform parent,
        float lineWidth,
        Material lineMaterial,
        Color lineColor)
    {
        while (debugLines.Count < BodySkeletonData.DebugConnectionSegmentCount)
        {
            GameObject lineObject = new($"BodyDebugLine_{debugLines.Count}");
            lineObject.transform.SetParent(parent, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = lineWidth;
            line.endWidth = lineWidth;
            line.sharedMaterial = lineMaterial;
            line.startColor = lineColor;
            line.endColor = lineColor;
            line.enabled = false;

            debugLines.Add(line);
        }
    }

    private static void UpdateConnectionSegment(
        LineRenderer line,
        BodyJointTracker tracker,
        BodyJointType startJoint,
        BodyJointType endJoint,
        float lineWidth)
    {
        if (!TryGetSegmentPositions(
                tracker,
                startJoint,
                endJoint,
                out Vector3 startPosition,
                out Vector3 endPosition))
        {
            line.enabled = false;
            return;
        }

        line.enabled = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.SetPosition(0, startPosition);
        line.SetPosition(1, endPosition);
    }

    private static bool TryGetSegmentPositions(
        BodyJointTracker tracker,
        BodyJointType startJoint,
        BodyJointType endJoint,
        out Vector3 startPosition,
        out Vector3 endPosition)
    {
        startPosition = default;
        endPosition = default;

        return tracker.IsJointTracked(startJoint)
            && tracker.IsJointTracked(endJoint)
            && tracker.TryGetSmoothedPosition(startJoint, out startPosition)
            && tracker.TryGetSmoothedPosition(endJoint, out endPosition);
    }
}
