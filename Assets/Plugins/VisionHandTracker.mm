#import <Foundation/Foundation.h>
#import <Vision/Vision.h>
#import <CoreGraphics/CoreGraphics.h>
#import <ImageIO/ImageIO.h>

extern "C" void UnitySendMessage(const char *obj, const char *method, const char *msg);

struct HandFrameImage
{
    CGImageRef image;
    CGDataProviderRef provider;
    CFDataRef data;
    CGColorSpaceRef colorSpace;
};

static bool IsValidInput(
    const unsigned char* imageData,
    int width,
    int height,
    const char* unityObjectName)
{
    return imageData != NULL && width > 0 && height > 0 && unityObjectName != NULL;
}

static void SendEmptyHandResult(const char* unityObjectName)
{
    UnitySendMessage(unityObjectName, "OnHandJointsDetected", "");
}

static HandFrameImage CreateHandFrameImage(const unsigned char* imageData, int width, int height)
{
    size_t bytesPerRow = width * 4;
    size_t dataSize = bytesPerRow * height;

    HandFrameImage frameImage;
    frameImage.colorSpace = CGColorSpaceCreateDeviceRGB();
    frameImage.data = CFDataCreate(kCFAllocatorDefault, imageData, dataSize);
    frameImage.provider = CGDataProviderCreateWithCFData(frameImage.data);
    frameImage.image = CGImageCreate(
        width,
        height,
        8,
        32,
        bytesPerRow,
        frameImage.colorSpace,
        kCGBitmapByteOrder32Big | kCGImageAlphaPremultipliedLast,
        frameImage.provider,
        NULL,
        false,
        kCGRenderingIntentDefault
    );

    return frameImage;
}

static void ReleaseHandFrameImage(HandFrameImage frameImage)
{
    if (frameImage.image != NULL)
    {
        CGImageRelease(frameImage.image);
    }

    if (frameImage.provider != NULL)
    {
        CGDataProviderRelease(frameImage.provider);
    }

    if (frameImage.data != NULL)
    {
        CFRelease(frameImage.data);
    }

    if (frameImage.colorSpace != NULL)
    {
        CGColorSpaceRelease(frameImage.colorSpace);
    }
}

static VNHumanHandPoseObservation *DetectHandPose(CGImageRef image)
{
    VNDetectHumanHandPoseRequest *request = [[VNDetectHumanHandPoseRequest alloc] init];
    request.maximumHandCount = 1;

    VNImageRequestHandler *handler = [[VNImageRequestHandler alloc]
        initWithCGImage:image
        orientation:kCGImagePropertyOrientationRight
        options:@{}
    ];

    NSError *error = nil;
    BOOL success = [handler performRequests:@[request] error:&error];

    if (!success || error != nil || request.results.count == 0)
    {
        return nil;
    }

    return request.results.firstObject;
}

static NSDictionary<VNHumanHandPoseObservationJointName, VNRecognizedPoint *> *
ExtractRecognizedHandPoints(VNHumanHandPoseObservation *observation)
{
    NSError *error = nil;

    NSDictionary<VNHumanHandPoseObservationJointName, VNRecognizedPoint *> *points =
        [observation recognizedPointsForJointsGroupName:VNHumanHandPoseObservationJointsGroupNameAll
                                                 error:&error];

    if (points == nil || error != nil)
    {
        return nil;
    }

    return points;
}

static NSArray<VNHumanHandPoseObservationJointName> *GetOrderedHandJointNames()
{
    return @[
        VNHumanHandPoseObservationJointNameWrist,

        VNHumanHandPoseObservationJointNameThumbCMC,
        VNHumanHandPoseObservationJointNameThumbMP,
        VNHumanHandPoseObservationJointNameThumbIP,
        VNHumanHandPoseObservationJointNameThumbTip,

        VNHumanHandPoseObservationJointNameIndexMCP,
        VNHumanHandPoseObservationJointNameIndexPIP,
        VNHumanHandPoseObservationJointNameIndexDIP,
        VNHumanHandPoseObservationJointNameIndexTip,

        VNHumanHandPoseObservationJointNameMiddleMCP,
        VNHumanHandPoseObservationJointNameMiddlePIP,
        VNHumanHandPoseObservationJointNameMiddleDIP,
        VNHumanHandPoseObservationJointNameMiddleTip,

        VNHumanHandPoseObservationJointNameRingMCP,
        VNHumanHandPoseObservationJointNameRingPIP,
        VNHumanHandPoseObservationJointNameRingDIP,
        VNHumanHandPoseObservationJointNameRingTip,

        VNHumanHandPoseObservationJointNameLittleMCP,
        VNHumanHandPoseObservationJointNameLittlePIP,
        VNHumanHandPoseObservationJointNameLittleDIP,
        VNHumanHandPoseObservationJointNameLittleTip
    ];
}

static void AppendJointToPayload(
    NSMutableString *payload,
    int jointIndex,
    VNRecognizedPoint *point)
{
    if (point == nil || point.confidence <= 0.0)
    {
        return;
    }

    [payload appendFormat:@"%d,%.5f,%.5f,%.5f|",
        jointIndex,
        point.location.x,
        point.location.y,
        point.confidence
    ];
}

static NSString *BuildHandJointPayload(
    NSDictionary<VNHumanHandPoseObservationJointName, VNRecognizedPoint *> *points)
{
    NSArray<VNHumanHandPoseObservationJointName> *jointNames = GetOrderedHandJointNames();
    NSMutableString *payload = [NSMutableString string];

    for (int i = 0; i < jointNames.count; i++)
    {
        VNHumanHandPoseObservationJointName jointName = jointNames[i];
        AppendJointToPayload(payload, i, points[jointName]);
    }

    return payload;
}

extern "C"
{
    void Vision_ProcessHandFrame(
        const unsigned char* imageData, int width, int height, const char* unityObjectName)
    {
        @autoreleasepool
        {
            if (!IsValidInput(imageData, width, height, unityObjectName))
            {
                return;
            }

            HandFrameImage frameImage = CreateHandFrameImage(imageData, width, height);

            if (frameImage.image == NULL)
            {
                SendEmptyHandResult(unityObjectName);
                ReleaseHandFrameImage(frameImage);
                return;
            }

            VNHumanHandPoseObservation *observation = DetectHandPose(frameImage.image);

            if (observation == nil)
            {
                SendEmptyHandResult(unityObjectName);
                ReleaseHandFrameImage(frameImage);
                return;
            }

            NSDictionary<VNHumanHandPoseObservationJointName, VNRecognizedPoint *> *points =
                ExtractRecognizedHandPoints(observation);

            if (points == nil)
            {
                SendEmptyHandResult(unityObjectName);
                ReleaseHandFrameImage(frameImage);
                return;
            }

            NSString *payload = BuildHandJointPayload(points);

            UnitySendMessage(unityObjectName, "OnHandJointsDetected", [payload UTF8String]);
            ReleaseHandFrameImage(frameImage);
        }
    }
}
