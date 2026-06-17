using System.Collections.Generic;
using UnityEngine;

public sealed class BoneColliderService
{
    private readonly float fallbackColliderPadding;

    public BoneColliderService(float fallbackColliderPadding)
    {
        this.fallbackColliderPadding = fallbackColliderPadding;
    }

    public void EnsureColliders(IEnumerable<Renderer> renderers)
    {
        if (renderers == null) return;
        
        foreach (Renderer renderer in renderers)
            EnsureCollider(renderer);
    }

    // ensure colliders are present on the 3D models, if not add them on runtime
    private void EnsureCollider(Renderer renderer)
    {
        if (renderer == null || renderer.GetComponent<Collider>() != null) return;

        Mesh sharedMesh = null;

        if (renderer.TryGetComponent(out MeshFilter meshFilter))
        {
            sharedMesh = meshFilter.sharedMesh;
        }
        else if (renderer is SkinnedMeshRenderer skinnedMeshRenderer)
        {
            sharedMesh = skinnedMeshRenderer.sharedMesh;
        }

        if (sharedMesh != null)
        {
            MeshCollider meshCollider = renderer.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = sharedMesh;
            return;
        }

        BoxCollider boxCollider = renderer.gameObject.AddComponent<BoxCollider>();
        boxCollider.center = renderer.localBounds.center;
        boxCollider.size = renderer.localBounds.size + Vector3.one * fallbackColliderPadding;
    }
}
