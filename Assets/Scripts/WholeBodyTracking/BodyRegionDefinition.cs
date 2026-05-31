using UnityEngine;

[System.Serializable]
public class BodyRegionDefinition
{
    public BodyRegion region;
    public string[] namePatterns;
    public Transform[] explicitObjects;
    public bool includeLeftRoot = true;
    public bool includeRightRoot = true;
}
