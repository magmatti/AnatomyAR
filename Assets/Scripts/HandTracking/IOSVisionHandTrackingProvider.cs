using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class IOSVisionHandTrackingProvider : MonoBehaviour
{
    [SerializeField] private IOSVisionHandFrameProcessing frameProcessing;
    [SerializeField] private HandJointTracker handJointTracker;
    [SerializeField] private Camera arCamera;

    [SerializeField] private float distanceFromCamera = 0.7f;
    [SerializeField] private float minimumConfidence = 0.3f;

    private void Update() => frameProcessing.ProcessFrameAtInterval(gameObject.name);

    // This method is called from the native iOS plugin using UnitySendMessage
    // Payload format:
    // jointIndex,x,y,confidence|jointIndex,x,y,confidence|...
    public void OnHandJointsDetected(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            handJointTracker.HideHand();
            return;
        }

        List<HandJointData> joints = ParseHandJoints(payload);

        if (joints.Count == 0)
        {
            handJointTracker.HideHand();
            return;
        }

        handJointTracker.UpdateHand(joints);
    }

    private List<HandJointData> ParseHandJoints(string payload)
    {
        List<HandJointData> joints = new();

        foreach (string entry in payload.Split('|'))
        {
            if (TryParseHandJoint(entry, out HandJointData joint))
            {
                joints.Add(joint);
            }
        }

        return joints;
    }

    private bool TryParseHandJoint(string entry, out HandJointData joint)
    {
        joint = default;

        if (string.IsNullOrWhiteSpace(entry))
        {
            return false;
        }

        string[] values = entry.Split(',');

        if (values.Length != 4)
        {
            return false;
        }

        if (!TryParseJointValues(values, out int jointIndex, out float x, out float y, 
            out float confidence))
        {
            return false;
        }

        if (!IsValidJoint(jointIndex, confidence))
        {
            return false;
        }

        joint = new HandJointData(
            (HandJointType)jointIndex,
            ConvertToWorldPosition(x, y),
            confidence
        );

        return true;
    }

    private static bool TryParseJointValues(
        string[] values,
        out int jointIndex,
        out float x,
        out float y,
        out float confidence
    )
    {
        (jointIndex, x, y, confidence) = (default, default, default, default);

        return int.TryParse(values[0], out jointIndex)
            && float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
            && float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
            && float.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, 
            out confidence);
    }

    private bool IsValidJoint(int jointIndex, float confidence)
    {
        return IsValidJointIndex(jointIndex) && confidence >= minimumConfidence;
    }

    private static bool IsValidJointIndex(int jointIndex)
    {
        return jointIndex >= (int)HandJointType.Wrist && jointIndex <= (int)HandJointType.LittleTip;
    }

    private Vector3 ConvertToWorldPosition(float x, float y)
    {
        Vector3 viewportPosition = new Vector3(x, y, distanceFromCamera);
        return arCamera.ViewportToWorldPoint(viewportPosition);
    }
}
