using System.Collections.Generic;
using UnityEngine;

public class HandSkeletonDebugVisualizer : MonoBehaviour
{
    [SerializeField] private HandJointTracker tracker;
    [SerializeField] private GameObject jointPrefab;
    [SerializeField] private bool showDebugVisualization = true;

    public bool DebugLinesVisible => showDebugVisualization;

    private readonly Dictionary<HandJointType, GameObject> jointObjects = new();
    private readonly List<LineRenderer> lineRenderers = new();

    public void SetDebugLinesVisible(bool isVisible)
    {
        showDebugVisualization = isVisible;
        UpdateDebugVisualization();
    }

    private void Start()
    {
        CreateJointObjects();
        CreateLineRenderers();
        HideDebugVisualization();
    }

    private void LateUpdate()
    {
        UpdateDebugVisualization();
    }

    private void UpdateDebugVisualization()
    {
        if (!showDebugVisualization || tracker == null || !tracker.IsHandVisible)
        {
            HideDebugVisualization();
            return;
        }

        UpdateJointObjects();
        UpdateLines();
    }

    private void UpdateJointObjects()
    {
        foreach (KeyValuePair<HandJointType, GameObject> jointObject in jointObjects)
        {
            Vector3 position = default;
            bool isTracked = tracker.IsJointTracked(jointObject.Key)
                && tracker.TryGetSmoothedPosition(jointObject.Key, out position);

            jointObject.Value.SetActive(isTracked);

            if (isTracked)
            {
                jointObject.Value.transform.position = position;
            }
        }
    }

    private void HideDebugVisualization()
    {
        foreach (GameObject jointObject in jointObjects.Values)
        {
            jointObject.SetActive(false);
        }

        foreach (LineRenderer line in lineRenderers)
        {
            line.enabled = false;
        }
    }

    private void CreateJointObjects()
    {
        for (int i = (int)HandJointType.Wrist; i <= (int)HandJointType.LittleTip; i++)
        {
            HandJointType jointType = (HandJointType)i;

            GameObject jointObject = Instantiate(jointPrefab, transform);
            jointObject.name = jointType.ToString();

            jointObjects.Add(jointType, jointObject);
        }
    }

    private void CreateLineRenderers()
    {
        foreach (IReadOnlyList<HandJointType> finger in HandJointConnections.FingerChains)
        {
            for (int i = 0; i < finger.Count - 1; i++)
            {
                GameObject lineObject = new GameObject($"Line_{finger[i]}_{finger[i + 1]}");
                lineObject.transform.SetParent(transform);

                LineRenderer line = lineObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.startWidth = 0.008f;
                line.endWidth = 0.008f;
                line.useWorldSpace = true;

                lineRenderers.Add(line);
            }
        }
    }

    private void UpdateLines()
    {
        int lineIndex = 0;

        foreach (IReadOnlyList<HandJointType> finger in HandJointConnections.FingerChains)
        {
            for (int i = 0; i < finger.Count - 1; i++)
            {
                HandJointType startJoint = finger[i];
                HandJointType endJoint = finger[i + 1];
                LineRenderer line = lineRenderers[lineIndex];

                Vector3 startPosition = default;
                Vector3 endPosition = default;

                bool canDrawLine = tracker.IsJointTracked(startJoint)
                    && tracker.IsJointTracked(endJoint)
                    && tracker.TryGetSmoothedPosition(startJoint, out startPosition)
                    && tracker.TryGetSmoothedPosition(endJoint, out endPosition);

                line.enabled = canDrawLine;

                if (canDrawLine)
                {
                    line.SetPosition(0, startPosition);
                    line.SetPosition(1, endPosition);
                }

                lineIndex++;
            }
        }
    }
}
