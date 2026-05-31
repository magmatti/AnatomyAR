using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class IOSVisionHandTrackingProvider : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private HandSkeletonVisualizer visualizer;
    [SerializeField] private Camera arCamera;

    [Header("Settings")]
    [SerializeField] private float detectionFps = 15f;
    [SerializeField] private float distanceFromCamera = 0.7f;
    [SerializeField] private float minimumConfidence = 0.3f;

    [Header("Coordinate Fixes")]
    [SerializeField] private bool mirrorX = false;
    [SerializeField] private bool flipY = false;

    private float nextDetectionTime;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Vision_ProcessHandFrame(
        IntPtr rgbaData,
        int width,
        int height,
        string unityObjectName
    );
#endif

    private void Awake()
    {
        if (arCamera == null)
        {
            arCamera = Camera.main;
        }

        if (cameraManager == null)
        {
            cameraManager = FindFirstObjectByType<ARCameraManager>();
        }

        if (visualizer == null)
        {
            visualizer = GetComponent<HandSkeletonVisualizer>();
        }
    }

    private void Update()
    {
    #if UNITY_IOS && !UNITY_EDITOR
            if (Time.time < nextDetectionTime)
            {
                return;
            }

            nextDetectionTime = Time.time + 1f / detectionFps;
            ProcessCameraFrame();
    #endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private unsafe void ProcessCameraFrame()
    {
        if (cameraManager == null)
        {
            return;
        }

        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        var outputDimensions = new Vector2Int(image.width, image.height);

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = outputDimensions,
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        image.Convert(
            conversionParams,
            new IntPtr(buffer.GetUnsafePtr()),
            buffer.Length
        );

        Vision_ProcessHandFrame(
            new IntPtr(buffer.GetUnsafePtr()),
            outputDimensions.x,
            outputDimensions.y,
            gameObject.name
        );

        buffer.Dispose();
        image.Dispose();
    }
#endif

    // This method is called from the native iOS plugin using UnitySendMessage.
    // Payload format:
    // jointIndex,x,y,confidence|jointIndex,x,y,confidence|...
    public void OnHandJointsDetected(string payload)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            visualizer.HideHand();
            return;
        }

        List<HandJointData> joints = new();

        string[] jointEntries = payload.Split('|');

        foreach (string entry in jointEntries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            string[] values = entry.Split(',');

            if (values.Length != 4)
            {
                continue;
            }

            if (!int.TryParse(values[0], out int jointIndex))
            {
                continue;
            }

            if (!float.TryParse(values[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
            {
                continue;
            }

            if (!float.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
            {
                continue;
            }

            if (!float.TryParse(values[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float confidence))
            {
                continue;
            }

            if (confidence < minimumConfidence)
            {
                continue;
            }

            if (jointIndex < 0 || jointIndex > 20)
            {
                continue;
            }

            if (mirrorX)
            {
                x = 1f - x;
            }

            if (flipY)
            {
                y = 1f - y;
            }

            Vector3 viewportPosition = new Vector3(x, y, distanceFromCamera);
            Vector3 worldPosition = arCamera.ViewportToWorldPoint(viewportPosition);

            joints.Add(new HandJointData(
                (HandJointType)jointIndex,
                worldPosition,
                confidence
            ));
        }

        if (joints.Count == 0)
        {
            visualizer.HideHand();
            return;
        }

        visualizer.UpdateHand(joints);
    }
}
