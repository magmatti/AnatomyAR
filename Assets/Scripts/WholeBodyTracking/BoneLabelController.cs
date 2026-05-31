using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using System.Collections.Generic;

[DefaultExecutionOrder(200)]
public class BoneLabelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private SkeletonRegionDisplayController skeletonController;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private bool renameKnownHandParts = false;

    [Header("Raycast")]
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float fallbackColliderPadding = 0.03f;

    [Header("Label")]
    [SerializeField] private Vector2 labelSize = new(560f, 96f);
    [SerializeField] private float bottomOffset = 64f;
    [SerializeField] private int fontSize = 40;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color backgroundColor = new(0f, 0f, 0f, 0.78f);
    [SerializeField] private float visibleSeconds = 2.5f;

    private Canvas canvas;
    private GameObject panel;
    private Text labelText;
    private string currentMessage = string.Empty;
    private float visibleUntil;
    private int lastHandledFrame = -1;
    private bool didRenameKnownParts;

    private readonly Dictionary<string, string> handBoneNameOverrides = new()
    {
        { "Palm", "Metacarpals" },
        { "FingerSeg7", "Index metacarpal" },
        { "FingerSeg8", "Middle metacarpal" },
        { "FingerSeg9", "Ring metacarpal" },
        { "FingerSeg10", "Little finger metacarpal" },
        { "pCube19", "Scaphoid" },
        { "pCube21", "Lunate" },
        { "pCube22", "Trapezium" },
        { "pCube23", "Trapezoid" },
        { "pCube24", "Capitate" },
        { "pCube25", "Hamate" },
        { "pCube26", "Pisiform" },
        { "Pointer", "Index proximal phalanx" },
        { "PointerFingerSeg2", "Index middle phalanx" },
        { "PointerFingerSeg3", "Index distal phalanx" },
        { "Middle", "Middle proximal phalanx" },
        { "MIddleFingerSeg2", "Middle middle phalanx" },
        { "MiddleFingerSeg2", "Middle middle phalanx" },
        { "MiddleFingerSeg3", "Middle distal phalanx" },
        { "Ring", "Ring proximal phalanx" },
        { "RingFingerSeg2", "Ring middle phalanx" },
        { "RingFingerSeg3", "Ring distal phalanx" },
        { "Pinky", "Little finger proximal phalanx" },
        { "PinkyFingerSeg2", "Little finger middle phalanx" },
        { "PinkyFingerSeg3", "Little finger distal phalanx" },
        { "Thumb", "Thumb metacarpal" },
        { "ThumbFingerSeg12", "Thumb proximal phalanx" },
        { "ThumbFingerSeg3", "Thumb distal phalanx" },
    };

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        EnhancedTouch.onFingerDown += HandleFingerDown;
    }

    private void OnDisable()
    {
        EnhancedTouch.onFingerDown -= HandleFingerDown;
        EnhancedTouchSupport.Disable();
    }

    private void Start()
    {
        ResolveReferences();
        RenameKnownModelPartsOnce();
        BuildLabelUi();
        EnsureSkeletonColliders();
        HideLabel();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryHandleTap(Mouse.current.position.ReadValue());
        }
    }

    private void HandleFingerDown(Finger finger)
    {
        TryHandleTap(finger.currentTouch.screenPosition);
    }

    private void TryHandleTap(Vector2 screenPosition)
    {
        if (lastHandledFrame == Time.frameCount)
        {
            return;
        }

        lastHandledFrame = Time.frameCount;
        ResolveReferences();

        if (arCamera == null)
        {
            HideLabel();
            return;
        }

        EnsureSkeletonColliders();

        Ray ray = arCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Max(maxRayDistance, 20f), raycastMask, QueryTriggerInteraction.Ignore);

        if (hits == null || hits.Length == 0)
        {
            HideLabel();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsSkeletonHit(hit.transform))
            {
                ShowMessage(GetBoneLabel(hit.transform));
                return;
            }
        }

        HideLabel();
    }

    private void ResolveReferences()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (skeletonController == null)
        {
            skeletonController = FindFirstObjectByType<SkeletonRegionDisplayController>(FindObjectsInactive.Include);
        }

        if (modelRoot == null && skeletonController == null)
        {
            GameObject fallbackRoot = GameObject.Find("SkeletonModelRoot");
            if (fallbackRoot != null)
            {
                modelRoot = fallbackRoot.transform;
            }
        }
    }

    private bool IsSkeletonHit(Transform candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        if (skeletonController != null)
        {
            return skeletonController.IsVisibleSkeletonPart(candidate);
        }

        return IsVisibleModelPart(candidate);
    }

    private string GetBoneLabel(Transform candidate)
    {
        if (skeletonController != null)
        {
            string cleanName = skeletonController.GetCleanBoneName(candidate);
            return string.IsNullOrWhiteSpace(cleanName)
                ? SkeletonRegionDisplayController.CleanBoneName(candidate.name)
                : cleanName;
        }

        string handName = GetHandBoneName(candidate);
        return string.IsNullOrWhiteSpace(handName)
            ? CleanModelName(candidate == null ? string.Empty : candidate.name)
            : handName;
    }

    private void EnsureSkeletonColliders()
    {
        if (skeletonController != null)
        {
            foreach (Renderer renderer in skeletonController.AllRenderers)
            {
                EnsureCollider(renderer);
            }

            return;
        }

        if (modelRoot == null)
        {
            return;
        }

        foreach (Renderer renderer in modelRoot.GetComponentsInChildren<Renderer>(true))
        {
            EnsureCollider(renderer);
        }
    }

    private void EnsureCollider(Renderer renderer)
    {
        if (renderer == null || renderer.GetComponent<Collider>() != null)
        {
            return;
        }

        Mesh sharedMesh = null;

        if (renderer.TryGetComponent(out MeshFilter meshFilter))
        {
            sharedMesh = meshFilter.sharedMesh;
        }
        else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            sharedMesh = skinnedMeshRenderer.sharedMesh;
        }

        if (sharedMesh != null)
        {
            MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = sharedMesh;
            return;
        }

        BoxCollider boxCollider = renderer.gameObject.AddComponent<BoxCollider>();
        boxCollider.center = renderer.localBounds.center;
        boxCollider.size = renderer.localBounds.size + Vector3.one * fallbackColliderPadding;
    }

    private bool IsVisibleModelPart(Transform candidate)
    {
        if (modelRoot == null || candidate == null || !candidate.IsChildOf(modelRoot))
        {
            return false;
        }

        Renderer renderer = candidate.GetComponent<Renderer>();
        return renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy;
    }

    private string GetHandBoneName(Transform candidate)
    {
        if (candidate == null)
        {
            return string.Empty;
        }

        if (handBoneNameOverrides.TryGetValue(candidate.name, out string exactName))
        {
            return exactName;
        }

        string inferredName = InferFingerBoneName(candidate);
        return string.IsNullOrWhiteSpace(inferredName) ? CleanModelName(candidate.name) : inferredName;
    }

    private string InferFingerBoneName(Transform candidate)
    {
        string fingerName = FindFingerName(candidate);
        if (string.IsNullOrWhiteSpace(fingerName))
        {
            return string.Empty;
        }

        string sourceName = candidate.name.ToLowerInvariant();

        if (fingerName == "Thumb")
        {
            if (sourceName.Contains("fingerseg3"))
            {
                return "Thumb distal phalanx";
            }

            if (sourceName.Contains("fingerseg"))
            {
                return "Thumb proximal phalanx";
            }

            return "Thumb metacarpal";
        }

        if (sourceName.Contains("fingerseg3"))
        {
            return $"{fingerName} distal phalanx";
        }

        if (sourceName.Contains("fingerseg2"))
        {
            return $"{fingerName} middle phalanx";
        }

        return $"{fingerName} proximal phalanx";
    }

    private string FindFingerName(Transform candidate)
    {
        for (Transform current = candidate; current != null && current != modelRoot.parent; current = current.parent)
        {
            string name = current.name.ToLowerInvariant();

            if (name.Contains("pointer") || name.Contains("index"))
            {
                return "Index";
            }

            if (name.Contains("middle"))
            {
                return "Middle";
            }

            if (name.Contains("ring"))
            {
                return "Ring";
            }

            if (name.Contains("pinky") || name.Contains("little"))
            {
                return "Little finger";
            }

            if (name.Contains("thumb"))
            {
                return "Thumb";
            }
        }

        return string.Empty;
    }

    private void RenameKnownModelPartsOnce()
    {
        if (didRenameKnownParts || !renameKnownHandParts || modelRoot == null)
        {
            return;
        }

        didRenameKnownParts = true;

        foreach (Transform child in modelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (child == modelRoot)
            {
                continue;
            }

            string anatomicalName = GetKnownHandBoneName(child);
            if (!string.IsNullOrWhiteSpace(anatomicalName) && anatomicalName != child.name)
            {
                child.name = anatomicalName;
            }
        }
    }

    private string GetKnownHandBoneName(Transform candidate)
    {
        if (candidate == null)
        {
            return string.Empty;
        }

        if (handBoneNameOverrides.TryGetValue(candidate.name, out string exactName))
        {
            return exactName;
        }

        return InferFingerBoneName(candidate);
    }

    private static string CleanModelName(string sourceName)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return string.Empty;
        }

        return sourceName
            .Replace("_", " ")
            .Replace(".r", string.Empty)
            .Replace(".l", string.Empty)
            .Trim();
    }

    private void BuildLabelUi()
    {
        if (canvas != null)
        {
            return;
        }

        Vector2 resolvedLabelSize = labelSize;
        int resolvedFontSize = fontSize;

        if (resolvedLabelSize.x < 80f || resolvedLabelSize.y < 24f)
        {
            resolvedLabelSize = new Vector2(560f, 96f);
        }

        if (resolvedFontSize < 10)
        {
            resolvedFontSize = 40;
        }

        GameObject canvasObject = new("BoneTapLabelCanvas");
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject("BoneTapLabelPanel");
        panel.transform.SetParent(canvasObject.transform, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, bottomOffset);
        panelRect.sizeDelta = resolvedLabelSize;

        Image background = panel.AddComponent<Image>();
        background.color = backgroundColor;
        background.raycastTarget = false;

        GameObject textObject = new("BoneTapLabelText");
        textObject.transform.SetParent(panel.transform, false);

        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 8f);
        textRect.offsetMax = new Vector2(-20f, -8f);

        labelText = textObject.AddComponent<Text>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (font != null)
        {
            labelText.font = font;
        }

        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = textColor;
        labelText.fontSize = resolvedFontSize;
        labelText.fontStyle = FontStyle.Bold;
        labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
        labelText.verticalOverflow = VerticalWrapMode.Truncate;
        labelText.raycastTarget = false;
    }

    private void ShowMessage(string message)
    {
        currentMessage = string.IsNullOrWhiteSpace(message) ? "(empty)" : message;
        visibleUntil = Time.unscaledTime + visibleSeconds;

        if (labelText != null)
        {
            labelText.text = currentMessage;
        }

        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void HideLabel()
    {
        currentMessage = string.Empty;

        if (labelText != null)
        {
            labelText.text = string.Empty;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (!string.IsNullOrEmpty(currentMessage) && Time.unscaledTime > visibleUntil)
        {
            HideLabel();
        }
    }

}
