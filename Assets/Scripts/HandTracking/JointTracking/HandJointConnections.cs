using System.Collections.Generic;

public static class HandJointConnections
{
    private static readonly IReadOnlyList<IReadOnlyList<HandJointType>> fingerChains =
        new IReadOnlyList<HandJointType>[]
        {
            new[]
            {
                HandJointType.Wrist,
                HandJointType.ThumbCMC,
                HandJointType.ThumbMP,
                HandJointType.ThumbIP,
                HandJointType.ThumbTip
            },
            new[]
            {
                HandJointType.Wrist,
                HandJointType.IndexMCP,
                HandJointType.IndexPIP,
                HandJointType.IndexDIP,
                HandJointType.IndexTip
            },
            new[]
            {
                HandJointType.Wrist,
                HandJointType.MiddleMCP,
                HandJointType.MiddlePIP,
                HandJointType.MiddleDIP,
                HandJointType.MiddleTip
            },
            new[]
            {
                HandJointType.Wrist,
                HandJointType.RingMCP,
                HandJointType.RingPIP,
                HandJointType.RingDIP,
                HandJointType.RingTip
            },
            new[]
            {
                HandJointType.Wrist,
                HandJointType.LittleMCP,
                HandJointType.LittlePIP,
                HandJointType.LittleDIP,
                HandJointType.LittleTip
            }
        };

    public static IReadOnlyList<IReadOnlyList<HandJointType>> FingerChains => fingerChains;
}
