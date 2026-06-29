using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BodyJointTracker : MonoBehaviour
{
    [SerializeField] private ARHumanBodyManager humanBodyManager;
    [SerializeField] private BodyJointIndexMapping[] jointMappings;
    [SerializeField] private float smoothingSpeed = 18f;

    public bool IsBodyVisible => jointCache.IsBodyVisible;

    private readonly BodyJointPoseCache jointCache = new();

    public bool TryGetSmoothedPosition(BodyJointType jointType, out Vector3 position)
    {
        return jointCache.TryGetSmoothedPosition(jointType, out position);
    }

    public bool TryGetTrackedRotation(BodyJointType jointType, out Quaternion rotation)
    {
        return jointCache.TryGetTrackedRotation(jointType, out rotation);
    }

    public bool IsJointTracked(BodyJointType jointType)
    {
        return jointCache.IsJointTracked(jointType);
    }

    private void Awake()
    {
        if (humanBodyManager == null)
        {
            humanBodyManager = FindFirstObjectByType<ARHumanBodyManager>();
        }

        EnsureDefaultJointMappings();
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
        if (args.updated.Count > 0)
        {
            UpdateBodyJoints(args.updated[0]);
            return;
        }

        if (args.added.Count > 0)
        {
            UpdateBodyJoints(args.added[0]);
            return;
        }

        if (args.removed.Count > 0)
        {
            HideBody();
        }
    }

    private void UpdateBodyJoints(ARHumanBody body)
    {
        jointCache.BeginFrame();

        NativeArray<XRHumanBodyJoint> joints = body.joints;

        foreach (BodyJointIndexMapping mapping in jointMappings)
        {
            if (BodyJointPoseReader.TryGetTrackedWorldPose(
                    body.transform,
                    joints,
                    mapping,
                    out Pose worldPose))
            {
                jointCache.TrackJoint(mapping.jointType, worldPose, smoothingSpeed, Time.deltaTime);
            }
        }

        jointCache.EndFrame();
    }

    private void HideBody()
    {
        jointCache.Clear();
    }

    private void EnsureDefaultJointMappings()
    {
        if (jointMappings != null && jointMappings.Length > 0)
        {
            return;
        }

        jointMappings = BodySkeletonData.CreateDefaultJointMappings();
    }
}
