using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BodyJointVisualizer : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARHumanBodyManager humanBodyManager;

    [Header("Joint Mapping")]
    [Tooltip("ARKit skeleton indices. These are exposed so they can be corrected in the Inspector if the provider changes skeleton definitions.")]
    [SerializeField] private BodyJointIndexMapping[] jointMappings;

    [Header("Debug Visualization")]
    [SerializeField] private GameObject jointPrefab;
    [SerializeField] private bool showDebugVisualization = false;
    [SerializeField] private bool drawDebugConnections = true;
    [SerializeField] private bool createDebugPrefabIfMissing = true;
    [SerializeField] private float debugJointSize = 0.04f;
    [SerializeField] private float debugLineWidth = 0.01f;
    [SerializeField] private Color debugJointColor = new(0.75f, 0.15f, 1f, 1f);
    [SerializeField] private Color debugLineColor = new(0.1f, 1f, 0.65f, 1f);

    [Header("Smoothing")]
    [SerializeField] private float smoothingSpeed = 18f;

    public bool IsBodyVisible { get; private set; }
    public bool DebugLinesVisible => showDebugVisualization && drawDebugConnections;

    private readonly Dictionary<BodyJointType, GameObject> jointObjects = new();
    private readonly Dictionary<BodyJointType, Vector3> smoothedPositions = new();
    private readonly List<LineRenderer> debugLines = new();
    private readonly HashSet<BodyJointType> trackedJoints = new();

    private readonly BodyJointType[][] bodyConnections =
    {
        new[] { BodyJointType.Head, BodyJointType.Neck },
        new[] { BodyJointType.Neck, BodyJointType.LeftShoulder },
        new[] { BodyJointType.Neck, BodyJointType.RightShoulder },
        new[] { BodyJointType.LeftShoulder, BodyJointType.RightShoulder },
        new[] { BodyJointType.LeftShoulder, BodyJointType.LeftElbow, BodyJointType.LeftWrist },
        new[] { BodyJointType.RightShoulder, BodyJointType.RightElbow, BodyJointType.RightWrist },
        new[] { BodyJointType.LeftShoulder, BodyJointType.LeftHip },
        new[] { BodyJointType.RightShoulder, BodyJointType.RightHip },
        new[] { BodyJointType.LeftHip, BodyJointType.RightHip },
        new[] { BodyJointType.LeftHip, BodyJointType.LeftKnee, BodyJointType.LeftAnkle },
        new[] { BodyJointType.RightHip, BodyJointType.RightKnee, BodyJointType.RightAnkle }
    };

    private Material debugJointMaterial;
    private Material debugLineMaterial;

    public bool TryGetSmoothedPosition(BodyJointType jointType, out Vector3 position)
    {
        if (trackedJoints.Contains(jointType) && smoothedPositions.TryGetValue(jointType, out position))
        {
            return true;
        }

        position = default;
        return false;
    }

    public void SetDebugLinesVisible(bool isVisible)
    {
        showDebugVisualization = isVisible;
        drawDebugConnections = isVisible;

        if (!isVisible)
        {
            HideDebugObjectsOnly();
            return;
        }

        UpdateDebugConnections();
    }

    private void Awake()
    {
        if (humanBodyManager == null)
        {
            humanBodyManager = FindFirstObjectByType<ARHumanBodyManager>();
        }

        EnsureDefaultJointMappings();
        EnsureDebugMaterials();
    }

    private void OnEnable()
    {
        if (humanBodyManager != null)
        {
            humanBodyManager.trackablesChanged.AddListener(OnHumanBodiesChanged);
        }
    }

    private void OnDisable()
    {
        if (humanBodyManager != null)
        {
            humanBodyManager.trackablesChanged.RemoveListener(OnHumanBodiesChanged);
        }
    }

    private void OnHumanBodiesChanged(ARTrackablesChangedEventArgs<ARHumanBody> args)
    {
        bool updatedAnyBody = false;

        foreach (ARHumanBody body in args.added)
        {
            UpdateBodyJoints(body);
            updatedAnyBody = true;
        }

        foreach (ARHumanBody body in args.updated)
        {
            UpdateBodyJoints(body);
            updatedAnyBody = true;
        }

        if (!updatedAnyBody && args.removed.Count > 0)
        {
            HideAllJoints();
        }
    }

    private void UpdateBodyJoints(ARHumanBody body)
    {
        trackedJoints.Clear();

        var joints = body.joints;

        foreach (BodyJointIndexMapping mapping in jointMappings)
        {
            if (mapping == null || mapping.arKitJointIndex < 0 || mapping.arKitJointIndex >= joints.Length)
            {
                continue;
            }

            XRHumanBodyJoint joint = joints[mapping.arKitJointIndex];

            if (!joint.tracked)
            {
                continue;
            }

            Vector3 worldPosition = body.transform.TransformPoint(joint.anchorPose.position);
            Quaternion worldRotation = body.transform.rotation * joint.anchorPose.rotation;

            if (!smoothedPositions.ContainsKey(mapping.jointType))
            {
                smoothedPositions[mapping.jointType] = worldPosition;
            }

            Vector3 previousPosition = smoothedPositions[mapping.jointType];
            Vector3 newPosition = Vector3.Lerp(previousPosition, worldPosition, Time.deltaTime * smoothingSpeed);

            smoothedPositions[mapping.jointType] = newPosition;
            trackedJoints.Add(mapping.jointType);

            if (showDebugVisualization && (jointPrefab != null || createDebugPrefabIfMissing))
            {
                GameObject jointObject = GetOrCreateJointObject(mapping.jointType);
                jointObject.transform.SetPositionAndRotation(newPosition, worldRotation);
                jointObject.transform.localScale = Vector3.one * Mathf.Max(0.001f, debugJointSize);
                jointObject.SetActive(true);
            }
        }

        IsBodyVisible = trackedJoints.Count > 0;
        HideUntrackedDebugJoints();
        UpdateDebugConnections();

        if (!showDebugVisualization)
        {
            HideDebugObjectsOnly();
        }
    }

    private GameObject GetOrCreateJointObject(BodyJointType jointType)
    {
        if (jointObjects.TryGetValue(jointType, out GameObject jointObject))
        {
            return jointObject;
        }

        jointObject = jointPrefab != null
            ? Instantiate(jointPrefab, transform)
            : CreateDefaultJointObject(jointType);

        jointObject.name = jointType.ToString();
        ConfigureDebugJointObject(jointObject);
        jointObjects.Add(jointType, jointObject);

        return jointObject;
    }

    private void HideUntrackedDebugJoints()
    {
        foreach (KeyValuePair<BodyJointType, GameObject> jointObject in jointObjects)
        {
            if (!trackedJoints.Contains(jointObject.Key))
            {
                jointObject.Value.SetActive(false);
            }
        }
    }

    private void HideAllJoints()
    {
        trackedJoints.Clear();
        IsBodyVisible = false;
        HideDebugObjectsOnly();
    }

    private void HideDebugObjectsOnly()
    {
        foreach (GameObject jointObject in jointObjects.Values)
        {
            jointObject.SetActive(false);
        }

        foreach (LineRenderer line in debugLines)
        {
            line.enabled = false;
        }
    }

    private void ConfigureDebugJointObject(GameObject jointObject)
    {
        if (jointObject == null)
        {
            return;
        }

        jointObject.transform.localScale = Vector3.one * Mathf.Max(0.001f, debugJointSize);

        Renderer renderer = jointObject.GetComponentInChildren<Renderer>();
        if (renderer != null && debugJointMaterial != null)
        {
            renderer.sharedMaterial = debugJointMaterial;
        }
    }

    private GameObject CreateDefaultJointObject(BodyJointType jointType)
    {
        GameObject jointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jointObject.name = jointType.ToString();
        jointObject.transform.SetParent(transform, false);
        ConfigureDebugJointObject(jointObject);
        return jointObject;
    }

    private void UpdateDebugConnections()
    {
        if (!showDebugVisualization || !drawDebugConnections)
        {
            foreach (LineRenderer line in debugLines)
            {
                line.enabled = false;
            }

            return;
        }

        EnsureDebugLines();

        int lineIndex = 0;

        foreach (BodyJointType[] connection in bodyConnections)
        {
            for (int i = 0; i < connection.Length - 1; i++)
            {
                BodyJointType startJoint = connection[i];
                BodyJointType endJoint = connection[i + 1];
                LineRenderer line = debugLines[lineIndex++];

                if (!trackedJoints.Contains(startJoint) || !trackedJoints.Contains(endJoint))
                {
                    line.enabled = false;
                    continue;
                }

                line.enabled = true;
                line.startWidth = debugLineWidth;
                line.endWidth = debugLineWidth;
                line.SetPosition(0, smoothedPositions[startJoint]);
                line.SetPosition(1, smoothedPositions[endJoint]);
            }
        }
    }

    private void EnsureDebugLines()
    {
        int requiredLineCount = 0;

        foreach (BodyJointType[] connection in bodyConnections)
        {
            requiredLineCount += Mathf.Max(0, connection.Length - 1);
        }

        while (debugLines.Count < requiredLineCount)
        {
            GameObject lineObject = new($"BodyDebugLine_{debugLines.Count}");
            lineObject.transform.SetParent(transform, false);

            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.startWidth = debugLineWidth;
            line.endWidth = debugLineWidth;
            line.sharedMaterial = debugLineMaterial;
            line.startColor = debugLineColor;
            line.endColor = debugLineColor;
            line.enabled = false;

            debugLines.Add(line);
        }
    }

    private void EnsureDebugMaterials()
    {
        if (debugJointMaterial == null)
        {
            debugJointMaterial = CreateDebugMaterial(debugJointColor);
        }

        if (debugLineMaterial == null)
        {
            debugLineMaterial = CreateDebugMaterial(debugLineColor);
        }
    }

    private static Material CreateDebugMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            return null;
        }

        Material material = new(shader);
        material.color = color;
        return material;
    }

    private void EnsureDefaultJointMappings()
    {
        if (jointMappings != null && jointMappings.Length > 0)
        {
            return;
        }

        jointMappings = new[]
        {
            new BodyJointIndexMapping(BodyJointType.LeftHip, 2),
            new BodyJointIndexMapping(BodyJointType.LeftKnee, 3),
            new BodyJointIndexMapping(BodyJointType.LeftAnkle, 4),
            new BodyJointIndexMapping(BodyJointType.RightHip, 7),
            new BodyJointIndexMapping(BodyJointType.RightKnee, 8),
            new BodyJointIndexMapping(BodyJointType.RightAnkle, 9),

            new BodyJointIndexMapping(BodyJointType.Neck, 18),
            new BodyJointIndexMapping(BodyJointType.LeftShoulder, 20),
            new BodyJointIndexMapping(BodyJointType.LeftElbow, 21),
            new BodyJointIndexMapping(BodyJointType.LeftWrist, 22),
            new BodyJointIndexMapping(BodyJointType.RightShoulder, 47),
            new BodyJointIndexMapping(BodyJointType.RightElbow, 48),
            new BodyJointIndexMapping(BodyJointType.RightWrist, 49),
            new BodyJointIndexMapping(BodyJointType.Head, 77)
        };
    }
}
