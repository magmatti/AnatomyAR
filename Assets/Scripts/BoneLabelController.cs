using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class BoneLabelController : MonoBehaviour
{
    [System.Serializable]
    public class BoneLabelEntry
    {
        public Transform bone;
        [TextArea] public string label;
    }

    [Header("Mapping")]
    [SerializeField] private BoneLabelEntry[] labels;

    [Header("References")]
    [SerializeField] private Camera arCamera;
    [Tooltip("Optional. If null a default material is created from Sprites/Default.")]
    [SerializeField] private Material lineMaterial;

    [Header("Placement")]
    [SerializeField] private float labelOffsetWorld = 0.05f;
    [SerializeField] private float maxRayDistance = 5f;
    [SerializeField] private LayerMask raycastMask = ~0;

    [Header("Style")]
    [SerializeField] private float fontSize = 0.4f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.85f);
    [SerializeField] private float lineWidth = 0.002f;
    [SerializeField] private Vector2 labelSize = new Vector2(0.3f, 0.1f);

    private GameObject labelInstance;
    private TextMeshPro labelText;
    private LineRenderer labelLine;
    private Transform currentBone;

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        EnsureColliders();
        BuildLabel();
        SetVisible(false);
    }

    private void Update()
    {
        Vector2? screenPos = null;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            screenPos = Mouse.current.position.ReadValue();
        }

        if (screenPos.HasValue)
        {
            HandleTap(screenPos.Value);
        }
    }

    private void LateUpdate()
    {
        if (currentBone == null || labelInstance == null || !labelInstance.activeSelf)
        {
            return;
        }

        if (arCamera == null)
        {
            return;
        }

        Vector3 bonePos = currentBone.position;
        Vector3 toCam = arCamera.transform.position - bonePos;

        if (toCam.sqrMagnitude < 1e-6f)
        {
            return;
        }

        toCam.Normalize();

        Vector3 camUp = arCamera.transform.up;
        Vector3 labelPos = bonePos + camUp * labelOffsetWorld;

        labelInstance.transform.position = labelPos;
        labelInstance.transform.rotation = Quaternion.LookRotation(-toCam, camUp);

        labelLine.SetPosition(0, bonePos);
        labelLine.SetPosition(1, labelPos);
    }

    private void HandleTap(Vector2 screenPos)
    {
        if (arCamera == null)
        {
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, raycastMask))
        {
            BoneLabelEntry entry = FindEntry(hit.transform);
            if (entry != null)
            {
                ShowLabel(entry);
                return;
            }
        }

        SetVisible(false);
    }

    private BoneLabelEntry FindEntry(Transform t)
    {
        if (labels == null)
        {
            return null;
        }

        while (t != null)
        {
            foreach (BoneLabelEntry entry in labels)
            {
                if (entry != null && entry.bone == t)
                {
                    return entry;
                }
            }
            t = t.parent;
        }

        return null;
    }

    private void ShowLabel(BoneLabelEntry entry)
    {
        currentBone = entry.bone;
        labelText.text = entry.label;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (labelInstance != null)
        {
            labelInstance.SetActive(visible);
        }

        if (!visible)
        {
            currentBone = null;
        }
    }

    private void EnsureColliders()
    {
        if (labels == null)
        {
            return;
        }

        foreach (BoneLabelEntry entry in labels)
        {
            if (entry == null || entry.bone == null)
            {
                continue;
            }

            if (entry.bone.GetComponent<Collider>() != null)
            {
                continue;
            }

            MeshFilter mf = entry.bone.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                Debug.LogWarning($"BoneLabelController: '{entry.bone.name}' has no MeshFilter/Mesh; cannot add collider.", entry.bone);
                continue;
            }

            MeshCollider mc = entry.bone.gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    private void BuildLabel()
    {
        labelInstance = new GameObject("BoneLabel");
        labelInstance.transform.SetParent(transform, false);

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(labelInstance.transform, false);

        labelText = textGO.AddComponent<TextMeshPro>();
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.fontSize = fontSize;
        labelText.color = textColor;
        labelText.text = string.Empty;

        RectTransform rt = textGO.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = labelSize;
        }

        labelLine = labelInstance.AddComponent<LineRenderer>();
        labelLine.useWorldSpace = true;
        labelLine.positionCount = 2;
        labelLine.startWidth = lineWidth;
        labelLine.endWidth = lineWidth;
        labelLine.startColor = lineColor;
        labelLine.endColor = lineColor;

        if (lineMaterial != null)
        {
            labelLine.material = lineMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                labelLine.material = new Material(shader);
            }
        }
    }
}
