using UnityEngine;

[System.Serializable]
public class BodyRegionDefinition
{
    public BodyRegionType region;
    public string[] namePatterns;
    public Transform[] explicitObjects;
    public bool includeLeftRoot = true;
    public bool includeRightRoot = true;
}
