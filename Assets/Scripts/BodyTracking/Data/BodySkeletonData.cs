using System.Collections.Generic;

public static class BodySkeletonData
{
    private static readonly BodyJointType[] debugJoints =
    {
        BodyJointType.Head,
        BodyJointType.Neck,
        BodyJointType.LeftShoulder,
        BodyJointType.LeftElbow,
        BodyJointType.LeftWrist,
        BodyJointType.RightShoulder,
        BodyJointType.RightElbow,
        BodyJointType.RightWrist,
        BodyJointType.LeftHip,
        BodyJointType.LeftKnee,
        BodyJointType.LeftAnkle,
        BodyJointType.RightHip,
        BodyJointType.RightKnee,
        BodyJointType.RightAnkle
    };

    private static readonly BodyJointType[][] bodyConnections =
    {
        new[] { BodyJointType.Head, BodyJointType.Neck },
        new[] { BodyJointType.Neck, BodyJointType.LeftShoulder },
        new[] { BodyJointType.Neck, BodyJointType.RightShoulder },
        new[] { BodyJointType.LeftShoulder, BodyJointType.RightShoulder },
        new[] { BodyJointType.LeftShoulder, BodyJointType.LeftElbow, BodyJointType.LeftWrist },
        new[] { BodyJointType.RightShoulder, BodyJointType.RightElbow, BodyJointType.RightWrist },
        new[] { BodyJointType.LeftShoulder, BodyJointType.LeftHip },
        new[] { BodyJointType.RightShoulder, BodyJointType.RightHip },
        new[] { BodyJointType.LeftHip, BodyJointType.RightHip },
        new[] { BodyJointType.LeftHip, BodyJointType.LeftKnee, BodyJointType.LeftAnkle },
        new[] { BodyJointType.RightHip, BodyJointType.RightKnee, BodyJointType.RightAnkle }
    };

    private static readonly BodyRegionJointMapping[] supportedRegionMappings =
    {
        new(BodyRegionType.Torso,
            BodyJointType.LeftShoulder, BodyJointType.RightShoulder, BodyJointType.LeftHip, 
            BodyJointType.RightHip),
        new(BodyRegionType.LeftThigh, BodyJointType.LeftHip, BodyJointType.LeftKnee),
        new(BodyRegionType.RightThigh, BodyJointType.RightHip, BodyJointType.RightKnee),
        new(BodyRegionType.LeftLowerLeg, BodyJointType.LeftKnee, BodyJointType.LeftAnkle),
        new(BodyRegionType.RightLowerLeg, BodyJointType.RightKnee, BodyJointType.RightAnkle)
    };

    public static IReadOnlyList<BodyJointType> DebugJoints => debugJoints;
    public static IReadOnlyList<BodyJointType[]> BodyConnections => bodyConnections;
    internal static IReadOnlyList<BodyRegionJointMapping> SupportedRegionMappings => 
        supportedRegionMappings;

    public static int DebugConnectionSegmentCount
    {
        get
        {
            int segmentCount = 0;

            foreach (BodyJointType[] connection in bodyConnections)
            {
                segmentCount += connection.Length - 1;
            }

            return segmentCount;
        }
    }

    public static BodyJointIndexMapping[] CreateDefaultJointMappings()
    {
        return new[]
        {
            new BodyJointIndexMapping(BodyJointType.LeftHip, 2),
            new BodyJointIndexMapping(BodyJointType.LeftKnee, 3),
            new BodyJointIndexMapping(BodyJointType.LeftAnkle, 4),
            new BodyJointIndexMapping(BodyJointType.RightHip, 7),
            new BodyJointIndexMapping(BodyJointType.RightKnee, 8),
            new BodyJointIndexMapping(BodyJointType.RightAnkle, 9),
            new BodyJointIndexMapping(BodyJointType.Neck, 18),
            new BodyJointIndexMapping(BodyJointType.LeftShoulder, 20),
            new BodyJointIndexMapping(BodyJointType.LeftElbow, 21),
            new BodyJointIndexMapping(BodyJointType.LeftWrist, 22),
            new BodyJointIndexMapping(BodyJointType.RightShoulder, 47),
            new BodyJointIndexMapping(BodyJointType.RightElbow, 48),
            new BodyJointIndexMapping(BodyJointType.RightWrist, 49),
            new BodyJointIndexMapping(BodyJointType.Head, 77)
        };
    }
}

internal sealed class BodyRegionJointMapping
{
    public BodyRegionType Region { get; }
    public BodyJointType[] Joints { get; }

    public BodyRegionJointMapping(BodyRegionType region, params BodyJointType[] joints)
    {
        Region = region;
        Joints = joints;
    }
}
