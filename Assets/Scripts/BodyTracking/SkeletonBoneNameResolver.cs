using UnityEngine;

public sealed class SkeletonBoneNameResolver
{
    public string GetBoneName(Transform candidate)
    {
        return candidate == null ? string.Empty : CleanName(candidate.name);
    }

    private static string CleanName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        string cleanName = rawName.Replace("_", " ").Trim().TrimEnd('.');

        if (cleanName.EndsWith(".r", System.StringComparison.OrdinalIgnoreCase) ||
            cleanName.EndsWith(".l", System.StringComparison.OrdinalIgnoreCase))
        {
            cleanName = cleanName.Substring(0, cleanName.Length - 2).Trim();
        }

        return cleanName.TrimEnd('.');
    }
}
