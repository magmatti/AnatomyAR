using UnityEngine;

public class HandModelController : MonoBehaviour
{
    [SerializeField] private HandJointTracker handJointTracker;
    [SerializeField] private Transform handModelRoot;
    [SerializeField] private Camera arCamera;

    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    [SerializeField] private Vector3 rotationOffsetEuler = Vector3.zero;
    [SerializeField] private float scaleMultiplier = 1f;

    [SerializeField] private bool scaleFromFingerTip = false;

    [SerializeField] private bool modelViewEnabled = true;

    [SerializeField] private float positionSmoothing = 18f;
    [SerializeField] private float rotationSmoothing = 18f;
    [SerializeField] private float scaleSmoothing = 12f;

    private bool initialized;
    private Vector3 currentPosition;
    private Quaternion currentRotation = Quaternion.identity;
    private Vector3 currentScale = Vector3.one;

    public bool ModelViewEnabled => modelViewEnabled;

    public void SetModelViewEnabled(bool isEnabled)
    {
        modelViewEnabled = isEnabled;

        if (!modelViewEnabled && handModelRoot != null && handModelRoot.gameObject.activeSelf)
        {
            handModelRoot.gameObject.SetActive(false);
        }

        initialized = false;
    }

    private void Awake()
    {
        if (handJointTracker == null)
        {
            handJointTracker = GetComponentInParent<HandJointTracker>();
        }

        if (arCamera == null)
        {
            arCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        if (handJointTracker == null || handModelRoot == null)
        {
            return;
        }

        if (!modelViewEnabled)
        {
            if (handModelRoot.gameObject.activeSelf)
            {
                handModelRoot.gameObject.SetActive(false);
            }

            initialized = false;
            return;
        }

        if (!handJointTracker.IsHandVisible)
        {
            if (handModelRoot.gameObject.activeSelf)
            {
                handModelRoot.gameObject.SetActive(false);
            }
            initialized = false;
            return;
        }

        if (!handJointTracker.TryGetSmoothedPosition(HandJointType.Wrist, out Vector3 wrist) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.IndexMCP, out Vector3 indexMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.MiddleMCP, out Vector3 middleMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.RingMCP, out Vector3 ringMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.LittleMCP, out Vector3 littleMCP))
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
        if (scaleFromFingerTip && handJointTracker.TryGetSmoothedPosition(HandJointType.MiddleTip, out Vector3 middleTip))
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
