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
    private readonly HandModelPlacementCalculator placementCalculator = new();

    public bool ModelViewEnabled => modelViewEnabled;

    public void SetModelViewEnabled(bool isEnabled)
    {
        modelViewEnabled = isEnabled;

        if (!modelViewEnabled)
        {
            HideModelAndReset();
            return;
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
        if (handJointTracker == null || handModelRoot == null) return;

        if (!modelViewEnabled || !handJointTracker.IsHandVisible)
        {
            HideModelAndReset();
            return;
        }

        if (!TryGetJointPose(out HandModelJointPose jointPose)) return;

        if (!placementCalculator.TryCalculateTargetPlacement(
                jointPose,
                positionOffset,
                rotationOffsetEuler,
                scaleMultiplier,
                scaleFromFingerTip,
                arCamera,
                out HandModelPlacement targetPlacement)) return;

        UpdateSmoothedPlacement(targetPlacement);
        ShowModel();
        ApplyCurrentPlacement();
    }

    private void HideModelAndReset()
    {
        if (handModelRoot != null && handModelRoot.gameObject.activeSelf)
        {
            handModelRoot.gameObject.SetActive(false);
        }

        initialized = false;
    }

    private bool TryGetJointPose(out HandModelJointPose jointPose)
    {
        jointPose = default;

        if (!handJointTracker.TryGetSmoothedPosition(HandJointType.Wrist, out Vector3 wrist) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.IndexMCP, out Vector3 indexMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.MiddleMCP, out Vector3 middleMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.RingMCP, out Vector3 ringMCP) ||
            !handJointTracker.TryGetSmoothedPosition(HandJointType.LittleMCP, out Vector3 littleMCP))
        {
            return false;
        }

        Vector3 middleTip = default;
        bool hasMiddleTip = scaleFromFingerTip &&
            handJointTracker.TryGetSmoothedPosition(HandJointType.MiddleTip, out middleTip);

        jointPose = new HandModelJointPose(
            wrist,
            indexMCP,
            middleMCP,
            ringMCP,
            littleMCP,
            hasMiddleTip,
            middleTip
        );

        return true;
    }

    private void UpdateSmoothedPlacement(HandModelPlacement targetPlacement)
    {
        if (!initialized)
        {
            InitializePlacement(targetPlacement);
        }
        else
        {
            SmoothPlacement(targetPlacement);
        }
    }

    private void InitializePlacement(HandModelPlacement targetPlacement)
    {
        currentPosition = targetPlacement.Position;
        currentRotation = targetPlacement.Rotation;
        currentScale = targetPlacement.Scale;
        initialized = true;
    }

    private void SmoothPlacement(HandModelPlacement targetPlacement)
    {
        currentPosition = Vector3.Lerp(
            currentPosition,
            targetPlacement.Position,
            Time.deltaTime * positionSmoothing
        );
        currentRotation = Quaternion.Slerp(
            currentRotation,
            targetPlacement.Rotation,
            Time.deltaTime * rotationSmoothing
        );
        currentScale = Vector3.Lerp(
            currentScale,
            targetPlacement.Scale,
            Time.deltaTime * scaleSmoothing
        );
    }

    private void ShowModel()
    {
        if (!handModelRoot.gameObject.activeSelf) handModelRoot.gameObject.SetActive(true);
    }

    private void ApplyCurrentPlacement()
    {
        handModelRoot.SetPositionAndRotation(currentPosition, currentRotation);
        handModelRoot.localScale = currentScale;
    }
}
