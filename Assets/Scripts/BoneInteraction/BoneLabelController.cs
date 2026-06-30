using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DefaultExecutionOrder(200)]
public class BoneLabelController : MonoBehaviour
{
    // references
    [SerializeField] private Camera arCamera;
    [SerializeField] private SkeletonModelController skeletonController;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private BoneLabelUIManager labelUIManager;

    // raycast settings
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float fallbackColliderPadding = 0.03f;

    // highlight color
    [SerializeField] private Color highlightColor = new(1f, 0.78f, 0.08f, 1f);

    private BoneTargetResolver targetResolver;
    private BoneColliderService colliderService;
    private BoneSelectionRaycaster selectionRaycaster;
    private BoneHighlighter highlighter;
    private HandBoneNameResolver handNameResolver;

    // guard to prevent multiple selection during single frame
    private int lastHandledFrame = -1;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        EnhancedTouch.onFingerDown += HandleFingerDown;
    }

    private void OnDisable()
    {
        EnhancedTouch.onFingerDown -= HandleFingerDown;
        EnhancedTouchSupport.Disable();
        highlighter?.Clear();
    }

    private void Start()
    {
        InitializeServices();
        HideSelection();
    }

    private void LateUpdate()
    {
        if (labelUIManager != null && labelUIManager.HideLabelIfTimeExpired())
        {
            highlighter?.Clear();
        }
    }

    private void InitializeServices()
    {
        handNameResolver ??= new HandBoneNameResolver();
        targetResolver ??= new BoneTargetResolver(arCamera, skeletonController, modelRoot, 
            handNameResolver);
        colliderService ??= new BoneColliderService(fallbackColliderPadding);
        selectionRaycaster ??= new BoneSelectionRaycaster(targetResolver, colliderService);
        highlighter ??= new BoneHighlighter(highlightColor);
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
        InitializeServices();

        if (selectionRaycaster
            .TrySelect(screenPosition, maxRayDistance, raycastMask, out BoneSelection selection))
        {
            ShowSelection(selection);
            return;
        }

        HideSelection();
    }

    private void ShowSelection(BoneSelection selection)
    {
        labelUIManager?.ShowLabel(selection.Label);
        highlighter?.Highlight(selection.BoneTransform);
    }

    private void HideSelection()
    {
        labelUIManager?.HideLabel();
        highlighter?.Clear();
    }
}
