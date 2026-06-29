using System.Collections.Generic;
using UnityEngine;

public sealed class BodyDebugJointRenderer
{
    private readonly Dictionary<BodyJointType, GameObject> jointObjects = new();

    public void UpdateJoints(
        BodyJointTracker tracker,
        Transform parent,
        GameObject jointPrefab,
        bool createDebugPrefabIfMissing,
        float jointSize,
        Material jointMaterial)
    {
        foreach (BodyJointType jointType in BodySkeletonData.DebugJoints)
        {
            if (!TryGetTrackedJointPose(tracker, jointType, out Pose jointPose))
            {
                HideJoint(jointType);
                continue;
            }

            if (!TryGetJointObject(
                jointType,
                parent,
                jointPrefab,
                createDebugPrefabIfMissing,
                jointMaterial,
                out GameObject jointObject))
            {
                continue;
            }

            RenderJoint(jointObject, jointPose, jointSize);
        }
    }

    public void HideJoints()
    {
        foreach (GameObject jointObject in jointObjects.Values)
        {
            jointObject.SetActive(false);
        }
    }

    private bool TryGetJointObject(
        BodyJointType jointType,
        Transform parent,
        GameObject jointPrefab,
        bool createDebugPrefabIfMissing,
        Material jointMaterial,
        out GameObject jointObject)
    {
        if (jointObjects.TryGetValue(jointType, out jointObject))
        {
            return jointObject != null;
        }

        if (!CanCreateJointObject(jointPrefab, createDebugPrefabIfMissing))
        {
            jointObject = null;
            return false;
        }

        jointObject = CreateJointObject(jointType, parent, jointPrefab);

        if (jointObject == null)
        {
            return false;
        }

        ApplyJointAppearance(jointObject, jointMaterial);
        jointObjects.Add(jointType, jointObject);

        return true;
    }

    private static bool TryGetTrackedJointPose(
        BodyJointTracker tracker,
        BodyJointType jointType,
        out Pose jointPose)
    {
        jointPose = default;

        if (!tracker.IsJointTracked(jointType)
            || !tracker.TryGetSmoothedPosition(jointType, out Vector3 position))
        {
            return false;
        }

        Quaternion rotation = tracker.TryGetTrackedRotation(jointType, out Quaternion trackedRotation)
            ? trackedRotation
            : Quaternion.identity;

        jointPose = new Pose(position, rotation);
        return true;
    }

    private static bool CanCreateJointObject(GameObject jointPrefab, bool createDebugPrefabIfMissing)
    {
        return jointPrefab != null || createDebugPrefabIfMissing;
    }

    private static GameObject CreateJointObject(
        BodyJointType jointType,
        Transform parent,
        GameObject jointPrefab)
    {
        GameObject jointObject = jointPrefab != null
            ? CreatePrefabJointObject(jointPrefab, parent)
            : CreateFallbackJointObject(parent);

        if (jointObject != null)
        {
            jointObject.name = jointType.ToString();
        }

        return jointObject;
    }

    private void HideJoint(BodyJointType jointType)
    {
        if (jointObjects.TryGetValue(jointType, out GameObject jointObject))
        {
            jointObject.SetActive(false);
        }
    }

    private static GameObject CreatePrefabJointObject(GameObject jointPrefab, Transform parent)
    {
        return Object.Instantiate(jointPrefab, parent);
    }

    private static GameObject CreateFallbackJointObject(Transform parent)
    {
        GameObject jointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        jointObject.transform.SetParent(parent, false);
        return jointObject;
    }

    private static void ApplyJointAppearance(GameObject jointObject, Material jointMaterial)
    {
        if (jointObject == null || jointMaterial == null)
        {
            return;
        }

        Renderer renderer = jointObject.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = jointMaterial;
        }
    }

    private static void RenderJoint(GameObject jointObject, Pose jointPose, float jointSize)
    {
        jointObject.transform.SetPositionAndRotation(jointPose.position, jointPose.rotation);
        jointObject.transform.localScale = Vector3.one * Mathf.Max(0.001f, jointSize);
        jointObject.SetActive(true);
    }
}
