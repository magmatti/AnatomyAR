using System.Collections.Generic;

public static class HandBoneNamesDictionary
{
    public static readonly IReadOnlyDictionary<string, string> Overrides = 
        new Dictionary<string, string>
    {
        { "Palm", "Metacarpals" },
        { "FingerSeg7", "Index metacarpal" },
        { "FingerSeg8", "Middle metacarpal" },
        { "FingerSeg9", "Ring metacarpal" },
        { "FingerSeg10", "Little finger metacarpal" },
        { "pCube19", "Scaphoid" },
        { "pCube21", "Lunate" },
        { "pCube22", "Trapezium" },
        { "pCube23", "Trapezoid" },
        { "pCube24", "Capitate" },
        { "pCube25", "Hamate" },
        { "pCube26", "Pisiform" },
        { "Pointer", "Index proximal phalanx" },
        { "PointerFingerSeg2", "Index middle phalanx" },
        { "PointerFingerSeg3", "Index distal phalanx" },
        { "Middle", "Middle proximal phalanx" },
        { "MIddleFingerSeg2", "Middle middle phalanx" },
        { "MIddleFingerSeg3", "Middle distal phalanx" },
        { "MiddleFingerSeg2", "Middle middle phalanx" },
        { "MiddleFingerSeg3", "Middle distal phalanx" },
        { "Ring", "Ring proximal phalanx" },
        { "RingFingerSeg2", "Ring middle phalanx" },
        { "RingFingerSeg3", "Ring distal phalanx" },
        { "Pinky", "Little finger proximal phalanx" },
        { "PinkyFingerSeg2", "Little finger middle phalanx" },
        { "PinkyFingerSeg3", "Little finger distal phalanx" },
        { "Thumb", "Thumb metacarpal" },
        { "ThumbFingerSeg12", "Thumb proximal phalanx" },
        { "ThumbFingerSeg3", "Thumb distal phalanx" },
    };
}
