using System;
using UnityEngine;

internal sealed class SkeletonRegionMatcher
{
    private readonly BodyRegionDefinition[] definitions;
    private readonly Transform leftRoot;
    private readonly Transform rightRoot;

    public SkeletonRegionMatcher(BodyRegionDefinition[] definitions, Transform leftRoot, Transform rightRoot)
    {
        this.definitions = definitions ?? Array.Empty<BodyRegionDefinition>();
        this.leftRoot = leftRoot;
        this.rightRoot = rightRoot;
    }

    public bool MatchesRegion(Transform part, BodyRegionType region)
    {
        BodyRegionDefinition definition = FindDefinition(region);
        return definition != null && MatchesDefinition(part, definition);
    }

    private BodyRegionDefinition FindDefinition(BodyRegionType region)
    {
        foreach (BodyRegionDefinition definition in definitions)
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
        if (part == null || !IsAllowedByRoot(part, definition))
        {
            return false;
        }

        return MatchesExplicitObject(part, definition) || MatchesNamePattern(part, definition);
    }

    private static bool MatchesExplicitObject(Transform part, BodyRegionDefinition definition)
    {
        if (definition.explicitObjects == null)
        {
            return false;
        }

        foreach (Transform explicitObject in definition.explicitObjects)
        {
            if (explicitObject != null && (part == explicitObject || part.IsChildOf(explicitObject)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesNamePattern(Transform part, BodyRegionDefinition definition)
    {
        if (definition.namePatterns == null)
        {
            return false;
        }

        string partName = part.name.ToLowerInvariant();

        foreach (string pattern in definition.namePatterns)
        {
            if (MatchesNamePattern(partName, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesNamePattern(string partName, string pattern)
    {
        return !string.IsNullOrWhiteSpace(pattern) &&
               partName.Contains(pattern.ToLowerInvariant());
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
}
