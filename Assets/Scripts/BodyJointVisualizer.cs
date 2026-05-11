using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class BodyJointVisualizer : MonoBehaviour
{
    [Header("AR")]
    [SerializeField] private ARHumanBodyManager humanBodyManager;

    [Header("Debug Visualization")]
    [SerializeField] private GameObject jointPrefab;

    private readonly Dictionary<int, GameObject> jointObjects = new();

    private void Awake()
    {
        if (humanBodyManager == null)
        {
            humanBodyManager = FindFirstObjectByType<ARHumanBodyManager>();
        }
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
        foreach (ARHumanBody body in args.added)
        {
            UpdateBodyJoints(body);
        }

        foreach (ARHumanBody body in args.updated)
        {
            UpdateBodyJoints(body);
        }

        foreach (var removedBody in args.removed)
        {
            HideAllJoints();
        }
    }

    private void UpdateBodyJoints(ARHumanBody body)
    {
        var joints = body.joints;

        for (int i = 0; i < joints.Length; i++)
        {
            XRHumanBodyJoint joint = joints[i];

            if (!joint.tracked)
            {
                continue;
            }

            if (!jointObjects.ContainsKey(i))
            {
                GameObject jointObject = Instantiate(jointPrefab);
                jointObject.name = $"Joint_{i}";
                jointObjects.Add(i, jointObject);
            }

            Vector3 worldPosition = body.transform.TransformPoint(joint.anchorPose.position);
            Quaternion worldRotation = body.transform.rotation * joint.anchorPose.rotation;

            jointObjects[i].transform.SetPositionAndRotation(worldPosition, worldRotation);
            jointObjects[i].SetActive(true);
        }
    }

    private void HideAllJoints()
    {
        foreach (GameObject jointObject in jointObjects.Values)
        {
            jointObject.SetActive(false);
        }
    }
}