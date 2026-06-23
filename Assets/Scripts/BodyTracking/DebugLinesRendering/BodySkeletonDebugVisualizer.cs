using UnityEngine;

public class BodySkeletonDebugVisualizer : MonoBehaviour
{
    [SerializeField] private BodyJointTracker tracker;
    [SerializeField] private GameObject jointPrefab;
    [SerializeField] private bool showDebugVisualization = false;
    [SerializeField] private bool drawDebugConnections = true;
    [SerializeField] private bool createDebugPrefabIfMissing = true;
    [SerializeField] private float debugJointSize = 0.04f;
    [SerializeField] private float debugLineWidth = 0.01f;
    [SerializeField] private Color debugJointColor = new(0.75f, 0.15f, 1f, 1f);
    [SerializeField] private Color debugLineColor = new(0.1f, 1f, 0.65f, 1f);

    public bool DebugLinesVisible => showDebugVisualization && drawDebugConnections;

    private readonly BodyDebugJointRenderer jointRenderer = new();
    private readonly BodyDebugLineRenderer lineRenderer = new();

    private Material debugJointMaterial;
    private Material debugLineMaterial;

    public void SetDebugLinesVisible(bool isVisible)
    {
        showDebugVisualization = isVisible;
        drawDebugConnections = isVisible;

        if (!isVisible)
        {
            HideDebugVisualization();
            return;
        }

        UpdateDebugVisualization();
    }

    private void Awake()
    {
        if (tracker == null)
        {
            tracker = GetComponent<BodyJointTracker>();
        }

        if (tracker == null)
        {
            tracker = FindFirstObjectByType<BodyJointTracker>();
        }

        EnsureDebugMaterials();
    }

    private void LateUpdate()
    {
        UpdateDebugVisualization();
    }

    private void UpdateDebugVisualization()
    {
        if (!showDebugVisualization || tracker == null || !tracker.IsBodyVisible)
        {
            HideDebugVisualization();
            return;
        }

        UpdateJointObjects();

        if (drawDebugConnections)
        {
            UpdateDebugConnections();
            return;
        }

        lineRenderer.HideLines();
    }

    private void UpdateJointObjects()
    {
        jointRenderer.UpdateJoints(
            tracker,
            transform,
            jointPrefab,
            createDebugPrefabIfMissing,
            debugJointSize,
            debugJointMaterial
        );
    }

    private void HideDebugVisualization()
    {
        jointRenderer.HideJoints();
        lineRenderer.HideLines();
    }

    private void UpdateDebugConnections()
    {
        lineRenderer.UpdateConnectionLines(
            tracker,
            transform,
            debugLineWidth,
            debugLineMaterial,
            debugLineColor
        );
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
        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return null;
        }

        Material material = new(shader)
        {
            color = color
        };
        return material;
    }
}
