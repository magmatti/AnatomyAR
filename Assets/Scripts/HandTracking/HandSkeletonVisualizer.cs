using System.Collections.Generic;
using UnityEngine;

public class HandSkeletonVisualizer : MonoBehaviour
{
    [Header("Debug Prefabs")]
    [SerializeField] private GameObject jointPrefab;

    [Header("Settings")]
    [SerializeField] private float minimumConfidence = 0.5f;
    [SerializeField] private float smoothingSpeed = 18f;
    [SerializeField] private bool showDebugVisualization = true;

    public bool IsHandVisible { get; private set; }

    public bool TryGetSmoothedPosition(HandJointType jointType, out Vector3 position)
    {
        return smoothedPositions.TryGetValue(jointType, out position);
    }

    private readonly Dictionary<HandJointType, GameObject> jointObjects = new();
    private readonly Dictionary<HandJointType, Vector3> smoothedPositions = new();
    private readonly HashSet<HandJointType> trackedJoints = new();

    private readonly HandJointType[][] handConnections =
    {
        new[] { HandJointType.Wrist, HandJointType.ThumbCMC, HandJointType.ThumbMP, HandJointType.ThumbIP, HandJointType.ThumbTip },
        new[] { HandJointType.Wrist, HandJointType.IndexMCP, HandJointType.IndexPIP, HandJointType.IndexDIP, HandJointType.IndexTip },
        new[] { HandJointType.Wrist, HandJointType.MiddleMCP, HandJointType.MiddlePIP, HandJointType.MiddleDIP, HandJointType.MiddleTip },
        new[] { HandJointType.Wrist, HandJointType.RingMCP, HandJointType.RingPIP, HandJointType.RingDIP, HandJointType.RingTip },
        new[] { HandJointType.Wrist, HandJointType.LittleMCP, HandJointType.LittlePIP, HandJointType.LittleDIP, HandJointType.LittleTip }
    };

    private readonly List<LineRenderer> lineRenderers = new();

    private void Start()
    {
        CreateJointObjects();
        CreateLineRenderers();
        HideHand();
    }

    public void UpdateHand(List<HandJointData> joints)
    {
        trackedJoints.Clear();

        foreach (HandJointData joint in joints)
        {
            if (joint.confidence < minimumConfidence)
            {
                continue;
            }

            if (!jointObjects.ContainsKey(joint.jointType))
            {
                continue;
            }

            if (!smoothedPositions.ContainsKey(joint.jointType))
            {
                smoothedPositions[joint.jointType] = joint.position;
            }

            Vector3 previousPosition = smoothedPositions[joint.jointType];

            Vector3 newPosition = Vector3.Lerp(
                previousPosition,
                joint.position,
                Time.deltaTime * smoothingSpeed
            );

            smoothedPositions[joint.jointType] = newPosition;
            trackedJoints.Add(joint.jointType);

            GameObject jointObject = jointObjects[joint.jointType];
            jointObject.transform.position = newPosition;
            jointObject.SetActive(showDebugVisualization);
        }

        IsHandVisible = trackedJoints.Count > 0;
        UpdateLines();
    }

    public void HideHand()
    {
        trackedJoints.Clear();
        IsHandVisible = false;

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
        for (int i = 0; i < 21; i++)
        {
            HandJointType jointType = (HandJointType)i;

            GameObject jointObject = Instantiate(jointPrefab, transform);
            jointObject.name = jointType.ToString();

            jointObjects.Add(jointType, jointObject);
        }
    }

    private void CreateLineRenderers()
    {
        foreach (HandJointType[] finger in handConnections)
        {
            for (int i = 0; i < finger.Length - 1; i++)
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

        foreach (HandJointType[] finger in handConnections)
        {
            for (int i = 0; i < finger.Length - 1; i++)
            {
                HandJointType startJoint = finger[i];
                HandJointType endJoint = finger[i + 1];

                LineRenderer line = lineRenderers[lineIndex];

                bool bothTracked = trackedJoints.Contains(startJoint) && trackedJoints.Contains(endJoint);

                if (showDebugVisualization && bothTracked)
                {
                    line.enabled = true;
                    line.SetPosition(0, smoothedPositions[startJoint]);
                    line.SetPosition(1, smoothedPositions[endJoint]);
                }
                else
                {
                    line.enabled = false;
                }

                lineIndex++;
            }
        }
    }
}