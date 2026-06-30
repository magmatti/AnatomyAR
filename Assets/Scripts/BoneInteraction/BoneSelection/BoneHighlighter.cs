using UnityEngine;

public sealed class BoneHighlighter
{
    private readonly Color highlightColor;
    private Renderer highlightedRenderer;

    // caching of original bone materials in order to restore them later
    private Material[] highlightedOriginalMaterials;
    private Material highlightMaterial;

    public BoneHighlighter(Color highlightColor)
    {
        this.highlightColor = highlightColor;
    }

    public void Highlight(Transform boneTransform)
    {
        Renderer targetRenderer = FindHighlightRenderer(boneTransform);

        if (targetRenderer == null || !targetRenderer.enabled)
        {
            Clear();
            return;
        }

        if (highlightedRenderer == targetRenderer) return;

        Material highlightMaterialInstance = GetHighlightMaterial();
        if (highlightMaterialInstance == null)
        {
            Clear();
            return;
        }

        Clear();
        ApplyHighlight(targetRenderer, highlightMaterialInstance);
    }

    private void ApplyHighlight(Renderer targetRenderer, Material highlightMaterialInstance)
    {
        highlightedRenderer = targetRenderer;
        highlightedOriginalMaterials = targetRenderer.sharedMaterials;

        Material[] replacementMaterials = new Material[highlightedOriginalMaterials.Length];

        for (int i = 0; i < replacementMaterials.Length; i++)
        {
            replacementMaterials[i] = highlightMaterialInstance;
        }

        targetRenderer.sharedMaterials = replacementMaterials;
    }

    public void Clear()
    {
        if (highlightedRenderer != null && highlightedOriginalMaterials != null)
        {
            highlightedRenderer.sharedMaterials = highlightedOriginalMaterials;
        }

        highlightedRenderer = null;
        highlightedOriginalMaterials = null;
    }

    private Renderer FindHighlightRenderer(Transform candidate)
    {
        if (candidate == null) return null;

        if (candidate.TryGetComponent(out Renderer renderer)) return renderer;

        return candidate.GetComponentInParent<Renderer>();
    }

    private Material GetHighlightMaterial()
    {
        if (highlightMaterial != null)
        {
            return highlightMaterial;
        }

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            return null;
        }

        highlightMaterial = new Material(shader)
        {
            color = highlightColor
        };

        return highlightMaterial;
    }
}
