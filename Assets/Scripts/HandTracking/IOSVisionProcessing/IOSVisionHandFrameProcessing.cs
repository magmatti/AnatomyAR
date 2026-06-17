using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class IOSVisionHandFrameProcessing : MonoBehaviour
{
    [SerializeField] private ARCameraManager cameraManager;
    [SerializeField] private float detectionFps = 15f;

    private float nextDetectionTime;
    private float DetectionInterval => 1f / Mathf.Max(1f, detectionFps);

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void Vision_ProcessHandFrame(
        IntPtr rgbaData,
        int width,
        int height,
        string unityObjectName
    );
#endif

    public void ProcessFrameAtInterval(string callbackObjectName)
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (Time.time < nextDetectionTime)
        {
            return;
        }

        nextDetectionTime = Time.time + DetectionInterval;
        SendLatestCameraImageToVision(callbackObjectName);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    private unsafe void SendLatestCameraImageToVision(string callbackObjectName)
    {
        if (!cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            return;
        }

        try
        {
            SendCpuImageToVision(image, callbackObjectName);
        }
        finally
        {
            image.Dispose();
        }
    }

    private unsafe void SendCpuImageToVision(XRCpuImage image, string callbackObjectName)
    {
        XRCpuImage.ConversionParams conversionParams = CreateRgbaConversionParams(image);
        int size = image.GetConvertedDataSize(conversionParams);
        var buffer = new NativeArray<byte>(size, Allocator.Temp);

        try
        {
            image.Convert(
                conversionParams,
                new IntPtr(buffer.GetUnsafePtr()),
                buffer.Length
            );

            Vision_ProcessHandFrame(
                new IntPtr(buffer.GetUnsafePtr()),
                conversionParams.outputDimensions.x,
                conversionParams.outputDimensions.y,
                callbackObjectName
            );
        }
        finally
        {
            buffer.Dispose();
        }
    }

    private static XRCpuImage.ConversionParams CreateRgbaConversionParams(XRCpuImage image)
    {
        var outputDimensions = new Vector2Int(image.width, image.height);

        return new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = outputDimensions,
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };
    }
#endif
}
