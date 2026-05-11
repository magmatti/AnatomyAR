#import <Foundation/Foundation.h>
#import <Vision/Vision.h>
#import <CoreGraphics/CoreGraphics.h>
#import <ImageIO/ImageIO.h>

extern "C"
{
    void UnitySendMessage(const char *obj, const char *method, const char *msg);

    void Vision_ProcessHandFrame(const unsigned char* imageData, int width, int height, const char* unityObjectName)
    {
        @autoreleasepool
        {
            if (imageData == NULL || width <= 0 || height <= 0 || unityObjectName == NULL)
            {
                return;
            }

            size_t bytesPerRow = width * 4;
            size_t dataSize = bytesPerRow * height;

            CGColorSpaceRef colorSpace = CGColorSpaceCreateDeviceRGB();
            CFDataRef data = CFDataCreate(kCFAllocatorDefault, imageData, dataSize);
            CGDataProviderRef provider = CGDataProviderCreateWithCFData(data);

            CGImageRef cgImage = CGImageCreate(
                width,
                height,
                8,
                32,
                bytesPerRow,
                colorSpace,
                kCGBitmapByteOrder32Big | kCGImageAlphaPremultipliedLast,
                provider,
                NULL,
                false,
                kCGRenderingIntentDefault
            );

            if (cgImage == NULL)
            {
                UnitySendMessage(unityObjectName, "OnHandJointsDetected", "");
                CGDataProviderRelease(provider);
                CFRelease(data);
                CGColorSpaceRelease(colorSpace);
                return;
            }

            VNDetectHumanHandPoseRequest *request = [[VNDetectHumanHandPoseRequest alloc] init];
            request.maximumHandCount = 1;

            NSError *error = nil;

            VNImageRequestHandler *handler = [[VNImageRequestHandler alloc]
                initWithCGImage:cgImage
                orientation:kCGImagePropertyOrientationRight
                options:@{}
            ];

            BOOL success = [handler performRequests:@[request] error:&error];

            if (!success || error != nil || request.results.count == 0)
            {
                UnitySendMessage(unityObjectName, "OnHandJointsDetected", "");

                CGImageRelease(cgImage);
                CGDataProviderRelease(provider);
                CFRelease(data);
                CGColorSpaceRelease(colorSpace);
                return;
            }

            VNHumanHandPoseObservation *observation = request.results.firstObject;

            NSDictionary<VNHumanHandPoseObservationJointName, VNRecognizedPoint *> *points =
                [observation recognizedPointsForJointsGroupName:VNHumanHandPoseObservationJointsGroupNameAll error:&error];

            if (points == nil || error != nil)
            {
                UnitySendMessage(unityObjectName, "OnHandJointsDetected", "");

                CGImageRelease(cgImage);
                CGDataProviderRelease(provider);
                CFRelease(data);
                CGColorSpaceRelease(colorSpace);
                return;
            }

            NSArray<VNHumanHandPoseObservationJointName> *jointNames = @[
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

            NSMutableString *payload = [NSMutableString string];

            for (int i = 0; i < jointNames.count; i++)
            {
                VNHumanHandPoseObservationJointName jointName = jointNames[i];
                VNRecognizedPoint *point = points[jointName];

                if (point == nil)
                {
                    continue;
                }

                if (point.confidence <= 0.0)
                {
                    continue;
                }

                [payload appendFormat:@"%d,%.5f,%.5f,%.5f|",
                    i,
                    point.location.x,
                    point.location.y,
                    point.confidence
                ];
            }

            UnitySendMessage(unityObjectName, "OnHandJointsDetected", [payload UTF8String]);

            CGImageRelease(cgImage);
            CGDataProviderRelease(provider);
            CFRelease(data);
            CGColorSpaceRelease(colorSpace);
        }
    }
}