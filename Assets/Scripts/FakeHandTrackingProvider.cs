using System.Collections.Generic;
using UnityEngine;

public class FakeHandTrackingProvider : MonoBehaviour
{
    [SerializeField] private HandSkeletonVisualizer visualizer;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float distanceFromCamera = 1.0f;
    [SerializeField] private float scale = 0.08f;

    private void Start()
    {
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        List<HandJointData> joints = GenerateFakeHand();
        visualizer.UpdateHand(joints);
    }

    private List<HandJointData> GenerateFakeHand()
    {
        Vector3 center = cameraTransform.position + cameraTransform.forward * distanceFromCamera;

        Vector3 right = cameraTransform.right;
        Vector3 up = cameraTransform.up;

        List<HandJointData> joints = new();

        joints.Add(new HandJointData(HandJointType.Wrist, center + up * -scale, 1f));

        AddFinger(joints, HandJointType.ThumbCMC, center + right * -scale * 1.2f, right * -0.04f + up * 0.03f);
        AddFinger(joints, HandJointType.IndexMCP, center + right * -scale * 0.6f, up * 0.05f);
        AddFinger(joints, HandJointType.MiddleMCP, center, up * 0.06f);
        AddFinger(joints, HandJointType.RingMCP, center + right * scale * 0.6f, up * 0.05f);
        AddFinger(joints, HandJointType.LittleMCP, center + right * scale * 1.2f, up * 0.04f);

        return joints;
    }

    private void AddFinger(List<HandJointData> joints, HandJointType startJoint, Vector3 startPosition, Vector3 direction)
    {
        int startIndex = (int)startJoint;

        for (int i = 0; i < 4; i++)
        {
            HandJointType jointType = (HandJointType)(startIndex + i);
            Vector3 position = startPosition + direction * i;

            joints.Add(new HandJointData(jointType, position, 1f));
        }
    }
}