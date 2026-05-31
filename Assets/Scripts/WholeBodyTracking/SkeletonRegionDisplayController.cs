using System.Collections.Generic;
using UnityEngine;

public class SkeletonRegionDisplayController : MonoBehaviour
{
    [Header("Hierarchy")]
    [SerializeField] private Transform skeletonRoot;
    [SerializeField] private string leftRootName = "left_skeleton";
    [SerializeField] private string rightRootName = "right_skeleton";

    [Header("Region Mapping")]
    [SerializeField] private BodyRegionDefinition[] regionDefinitions;
    [SerializeField] private bool buildDefaultMappingsWhenEmpty = true;
    [SerializeField] private bool hideOnStart = true;

    [Header("Tracked Placement")]
    [SerializeField] private bool followTrackedRegion = true;
    [SerializeField] private float trackedScaleMultiplier = 1.35f;
    [SerializeField] private float minimumScale = 0.05f;
    [SerializeField] private float maximumScale = 5f;
    [SerializeField] private float positionSmoothing = 18f;
    [SerializeField] private float scaleSmoothing = 14f;
    [Tooltip("Offset in camera-relative meters after the detected region has been centered. X/Y follow camera right/up, Z follows camera forward.")]
    [SerializeField] private Vector3 cameraRelativeOffset = Vector3.zero;

    private readonly List<Renderer> allRenderers = new();
    private readonly HashSet<Transform> visibleParts = new();
    private Transform leftRoot;
    private Transform rightRoot;
    private bool placementInitialized;
    private Vector3 currentPosition;
    private Vector3 currentScale;

    public BodyRegion? CurrentRegion { get; private set; }

    public IReadOnlyList<Renderer> AllRenderers => allRenderers;

    private void Awake()
    {
        if (skeletonRoot == null)
        {
            skeletonRoot = transform;
        }

        leftRoot = FindChildByName(skeletonRoot, leftRootName);
        rightRoot = FindChildByName(skeletonRoot, rightRootName);

        if (buildDefaultMappingsWhenEmpty && (regionDefinitions == null || regionDefinitions.Length == 0))
        {
            regionDefinitions = CreateDefaultDefinitions();
        }

        CollectRenderers();

        if (hideOnStart)
        {
            HideAll();
        }
    }

    public void ShowRegion(BodyRegion region)
    {
        if (CurrentRegion != region)
        {
            placementInitialized = false;
        }

        CurrentRegion = region;
        visibleParts.Clear();

        BodyRegionDefinition definition = FindDefinition(region);

        foreach (Renderer renderer in allRenderers)
        {
            bool visible = definition != null && MatchesDefinition(renderer.transform, definition);
            renderer.enabled = visible;

            if (visible)
            {
                visibleParts.Add(renderer.transform);
            }
        }
    }

    public void HideAll()
    {
        CurrentRegion = null;
        placementInitialized = false;
        visibleParts.Clear();

        foreach (Renderer renderer in allRenderers)
        {
            renderer.enabled = false;
        }
    }

    public void AlignVisibleRegion(Vector3 targetCenter, float targetWorldSpan, Camera arCamera)
    {
        if (!followTrackedRegion || visibleParts.Count == 0 || targetWorldSpan <= 1e-4f)
        {
            return;
        }

        if (!TryGetVisibleLocalBounds(out Bounds localBounds))
        {
            return;
        }

        float localSpan = Mathf.Max(localBounds.size.x, localBounds.size.y, localBounds.size.z);

        if (localSpan <= 1e-4f)
        {
            return;
        }

        float targetUniformScale = Mathf.Clamp(
            targetWorldSpan * trackedScaleMultiplier / localSpan,
            minimumScale,
            maximumScale
        );

        Vector3 targetScale = Vector3.one * targetUniformScale;
        Vector3 offset = GetCameraRelativeOffset(arCamera);
        Vector3 targetPosition = targetCenter + offset - skeletonRoot.rotation * (localBounds.center * targetUniformScale);

        if (!placementInitialized)
        {
            currentPosition = targetPosition;
            currentScale = targetScale;
            placementInitialized = true;
        }
        else
        {
            currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * positionSmoothing);
            currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * scaleSmoothing);
        }

        skeletonRoot.position = currentPosition;
        skeletonRoot.localScale = currentScale;
    }

    public bool IsVisibleSkeletonPart(Transform candidate)
    {
        while (candidate != null && candidate != skeletonRoot)
        {
            if (visibleParts.Contains(candidate))
            {
                return true;
            }

            candidate = candidate.parent;
        }

        return false;
    }

    public string GetCleanBoneName(Transform candidate)
    {
        Transform skeletonPart = GetSkeletonPart(candidate);
        return skeletonPart == null ? string.Empty : CleanBoneName(skeletonPart.name);
    }

    public static string CleanBoneName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string cleanName = rawName.Replace("_", " ").Trim();

        if (cleanName.EndsWith(".r", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.EndsWith(".l", System.StringComparison.OrdinalIgnoreCase))
        {
            cleanName = cleanName.Substring(0, cleanName.Length - 2).Trim();
        }

        return cleanName;
    }

    private Transform GetSkeletonPart(Transform candidate)
    {
        while (candidate != null && candidate != skeletonRoot)
        {
            if (visibleParts.Contains(candidate))
            {
                return candidate;
            }

            candidate = candidate.parent;
        }

        return null;
    }

    private bool TryGetVisibleLocalBounds(out Bounds localBounds)
    {
        localBounds = default;
        bool hasBounds = false;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            Bounds rendererBounds = renderer.bounds;
            Vector3 min = rendererBounds.min;
            Vector3 max = rendererBounds.max;

            Vector3[] corners =
            {
                new(min.x, min.y, min.z),
                new(min.x, min.y, max.z),
                new(min.x, max.y, min.z),
                new(min.x, max.y, max.z),
                new(max.x, min.y, min.z),
                new(max.x, min.y, max.z),
                new(max.x, max.y, min.z),
                new(max.x, max.y, max.z)
            };

            foreach (Vector3 corner in corners)
            {
                Vector3 localCorner = skeletonRoot.InverseTransformPoint(corner);

                if (!hasBounds)
                {
                    localBounds = new Bounds(localCorner, Vector3.zero);
                    hasBounds = true;
                }
                else
                {
                    localBounds.Encapsulate(localCorner);
                }
            }
        }

        return hasBounds;
    }

    private Vector3 GetCameraRelativeOffset(Camera arCamera)
    {
        if (arCamera == null)
        {
            return cameraRelativeOffset;
        }

        Transform cameraTransform = arCamera.transform;

        return cameraTransform.right * cameraRelativeOffset.x +
               cameraTransform.up * cameraRelativeOffset.y +
               cameraTransform.forward * cameraRelativeOffset.z;
    }

    private void CollectRenderers()
    {
        allRenderers.Clear();
        skeletonRoot.GetComponentsInChildren(true, allRenderers);
    }

    private BodyRegionDefinition FindDefinition(BodyRegion region)
    {
        if (regionDefinitions == null)
        {
            return null;
        }

        foreach (BodyRegionDefinition definition in regionDefinitions)
        {
            if (definition != null && definition.region == region)
            {
                return definition;
            }
        }

        return null;
    }

    private bool MatchesDefinition(Transform part, BodyRegionDefinition definition)
    {
        if (!IsAllowedByRoot(part, definition))
        {
            return false;
        }

        if (definition.explicitObjects != null)
        {
            foreach (Transform explicitObject in definition.explicitObjects)
            {
                if (explicitObject != null && (part == explicitObject || part.IsChildOf(explicitObject)))
                {
                    return true;
                }
            }
        }

        if (definition.namePatterns == null)
        {
            return false;
        }

        string partName = part.name.ToLowerInvariant();

        foreach (string pattern in definition.namePatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            if (partName.Contains(pattern.ToLowerInvariant()))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsAllowedByRoot(Transform part, BodyRegionDefinition definition)
    {
        bool isLeft = leftRoot != null && part.IsChildOf(leftRoot);
        bool isRight = rightRoot != null && part.IsChildOf(rightRoot);

        if (isLeft)
        {
            return definition.includeLeftRoot;
        }

        if (isRight)
        {
            return definition.includeRightRoot;
        }

        return true;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static BodyRegionDefinition[] CreateDefaultDefinitions()
    {
        return new[]
        {
            Definition(BodyRegion.Head, true, true,
                "atlas", "axis", "cervical vertebrae", "ethmoid", "frontal", "mandible",
                "maxilla", "nasal", "zygomatic", "temporal", "parietal", "occipital",
                "sphenoid", "vomer", "lacrimal", "palatine", "nasal concha", "tooth",
                "canine", "incisor", "molar", "premolar"),

            Definition(BodyRegion.Torso, true, true,
                "rib", "thoracic vertebrae", "lumbar vertebrae", "sternum", "costal cart",
                "sacrum", "coccyx", "clavicle", "scapula", "hip bone"),

            Definition(BodyRegion.LeftUpperArm, true, false, "humerus", "scapula", "clavicle"),
            Definition(BodyRegion.RightUpperArm, false, true, "humerus", "scapula", "clavicle"),
            Definition(BodyRegion.LeftForearm, true, false, "radius", "ulna"),
            Definition(BodyRegion.RightForearm, false, true, "radius", "ulna"),
            Definition(BodyRegion.LeftHand, true, false,
                "metacarpal", "capitate", "hamate", "lunate", "pisiform", "scaphoid",
                "trapezium", "trapezoid", "triquetrum", "phalanx of 1st finger",
                "phalanx of 2d finger", "phalanx of 3", "phalanx of 4th finger",
                "phalanx of 5th finger", "sesamoid_bones_of_hand", "sesamoid bones of hand"),
            Definition(BodyRegion.RightHand, false, true,
                "metacarpal", "capitate", "hamate", "lunate", "pisiform", "scaphoid",
                "trapezium", "trapezoid", "triquetrum", "phalanx of 1st finger",
                "phalanx of 2d finger", "phalanx of 3", "phalanx of 4th finger",
                "phalanx of 5th finger", "sesamoid_bones_of_hand", "sesamoid bones of hand"),

            Definition(BodyRegion.LeftThigh, true, false, "femur", "hip bone", "patella"),
            Definition(BodyRegion.RightThigh, false, true, "femur", "hip bone", "patella"),
            Definition(BodyRegion.LeftLowerLeg, true, false, "tibia", "fibula", "patella"),
            Definition(BodyRegion.RightLowerLeg, false, true, "tibia", "fibula", "patella"),
            Definition(BodyRegion.LeftFoot, true, false,
                "talus", "calcaneus", "navicular", "cuboid", "cuneiform", "metatarsal",
                "of foot", "sesamoid bones of foot"),
            Definition(BodyRegion.RightFoot, false, true,
                "talus", "calcaneus", "navicular", "cuboid", "cuneiform", "metatarsal",
                "of foot", "sesamoid bones of foot")
        };
    }

    private static BodyRegionDefinition Definition(BodyRegion region, bool includeLeftRoot, bool includeRightRoot, params string[] patterns)
    {
        return new BodyRegionDefinition
        {
            region = region,
            includeLeftRoot = includeLeftRoot,
            includeRightRoot = includeRightRoot,
            namePatterns = patterns
        };
    }
}
