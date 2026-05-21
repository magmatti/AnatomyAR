using UnityEngine;

public class TrackedHandModelController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HandSkeletonVisualizer visualizer;
    [SerializeField] private Transform handModelRoot;
    [SerializeField] private Camera arCamera;

    [Header("Offsets")]
    [Tooltip("In hand-size units. X/Y in the hand's local frame (X = across palm toward little, Y = wrist->fingers). Z is in CAMERA direction (positive = toward camera), so depth tuning stays consistent between palm and back views.")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;
    [SerializeField] private float scaleMultiplier = 1f;

    [Header("Scale Source")]
    [Tooltip("If true, scale uses Wrist->MiddleTip distance. If false, uses Wrist->MiddleMCP (more stable when fingers are bent).")]
    [SerializeField] private bool scaleFromFingerTip = false;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothing = 18f;
    [SerializeField] private float rotationSmoothing = 18f;
    [SerializeField] private float scaleSmoothing = 12f;

    private bool initialized;
    private Vector3 currentPosition;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentScale = Vector3.one;

    private void Awake()
    {
        if (visualizer == null)
        {
            visualizer = GetComponentInParent<HandSkeletonVisualizer>();
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (visualizer == null || handModelRoot == null)
        {
            return;
        }

        if (!visualizer.IsHandVisible)
        {
            if (handModelRoot.gameObject.activeSelf)
            {
                handModelRoot.gameObject.SetActive(false);
            }
            initialized = false;
            return;
        }

        if (!visualizer.TryGetSmoothedPosition(HandJointType.Wrist, out Vector3 wrist) ||
            !visualizer.TryGetSmoothedPosition(HandJointType.IndexMCP, out Vector3 indexMCP) ||
            !visualizer.TryGetSmoothedPosition(HandJointType.MiddleMCP, out Vector3 middleMCP) ||
            !visualizer.TryGetSmoothedPosition(HandJointType.RingMCP, out Vector3 ringMCP) ||
            !visualizer.TryGetSmoothedPosition(HandJointType.LittleMCP, out Vector3 littleMCP))
        {
            return;
        }

        Vector3 palmCenter = (wrist + indexMCP + middleMCP + ringMCP + littleMCP) * 0.2f;

        Vector3 handUp = (middleMCP - wrist);
        Vector3 acrossPalm = (littleMCP - indexMCP);

        if (handUp.sqrMagnitude < 1e-6f || acrossPalm.sqrMagnitude < 1e-6f)
        {
            return;
        }

        handUp.Normalize();
        acrossPalm.Normalize();

        Vector3 handForward = Vector3.Cross(acrossPalm, handUp);

        if (handForward.sqrMagnitude < 1e-6f)
        {
            return;
        }

        handForward.Normalize();

        Quaternion baseRotation = Quaternion.LookRotation(handForward, handUp);
        Quaternion targetRotation = baseRotation * Quaternion.Euler(rotationOffsetEuler);

        float handSize;
        if (scaleFromFingerTip && visualizer.TryGetSmoothedPosition(HandJointType.MiddleTip, out Vector3 middleTip))
        {
            handSize = Vector3.Distance(wrist, middleTip);
        }
        else
        {
            handSize = Vector3.Distance(wrist, middleMCP);
        }

        Vector3 targetScale = Vector3.one * (handSize * scaleMultiplier);

        Vector3 inPlaneOffset = baseRotation * new Vector3(positionOffset.x, positionOffset.y, 0f);
        Vector3 depthOffset = Vector3.zero;

        if (arCamera != null && Mathf.Abs(positionOffset.z) > 1e-6f)
        {
            Vector3 toCamera = arCamera.transform.position - palmCenter;
            if (toCamera.sqrMagnitude > 1e-6f)
            {
                depthOffset = toCamera.normalized * positionOffset.z;
            }
        }

        Vector3 targetPosition = palmCenter + (inPlaneOffset + depthOffset) * handSize;

        if (!initialized)
        {
            currentPosition = targetPosition;
            currentRotation = targetRotation;
            currentScale = targetScale;
            initialized = true;
        }
        else
        {
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * positionSmoothing);
            currentRotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * rotationSmoothing);
            currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * scaleSmoothing);
        }

        if (!handModelRoot.gameObject.activeSelf)
        {
            handModelRoot.gameObject.SetActive(true);
        }

        handModelRoot.SetPositionAndRotation(currentPosition, currentRotation);
        handModelRoot.localScale = currentScale;
    }
}
