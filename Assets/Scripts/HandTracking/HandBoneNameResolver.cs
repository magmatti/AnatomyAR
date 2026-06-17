using System.Collections.Generic;
using UnityEngine;

public sealed class HandBoneNameResolver
{
    private readonly IReadOnlyDictionary<string, string> handBoneNameOverrides = 
        HandBoneNamesDictionary.Overrides;

    public string GetBoneName(Transform candidate, Transform modelRoot)
    {
        if (candidate == null)
        {
            return string.Empty;
        }

        if (handBoneNameOverrides.TryGetValue(candidate.name, out string exactName))
        {
            return exactName;
        }

        if (GetNameFromPath(candidate, modelRoot, out string pathName))
        {
            return pathName;
        }

        return candidate.name;
    }

    private bool GetNameFromPath(Transform candidate, Transform modelRoot, out string boneName)
    {
        boneName = string.Empty;
        Transform stopParent = modelRoot == null ? null : modelRoot.parent;
        List<string> pathSegments = new();

        for (Transform current = candidate; current != null && current != stopParent; current = current.parent)
        {
            pathSegments.Insert(0, current.name);
        }

        for (int startIndex = 0; startIndex < pathSegments.Count; startIndex++)
        {
            string path = string
                .Join("/", pathSegments.GetRange(startIndex, pathSegments.Count - startIndex));
            
            if (handBoneNameOverrides.TryGetValue(path, out boneName))
            {
                return true;
            }
        }

        return false;
    }
}
