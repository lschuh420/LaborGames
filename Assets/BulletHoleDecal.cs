using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Spawns and manages persistent bullet-hole decals at impact points.
//
// Fully procedural: no prefab, material or texture asset has to be wired up in the
// Inspector. Just call BulletHoleDecal.Spawn(point, normal, surface) from your shooting
// code (see PlayerShooting.cs). The quad mesh, transparent material and scorched-hole
// texture are all built once at runtime and shared between every hole.
public class BulletHoleDecal : MonoBehaviour
{
    [Tooltip("World-space diameter of the hole in meters.")]
    public float size = 0.15f;
    [Tooltip("Seconds the hole stays fully visible before it begins to fade.")]
    public float lifetime = 20f;
    [Tooltip("How long the fade-out takes once lifetime is reached.")]
    public float fadeDuration = 3f;

    // Cap how many holes are alive at once so the scene doesn't fill up with decals.
    // The oldest one is recycled when the cap is reached.
    const int MaxHoles = 100;
    static readonly Queue<BulletHoleDecal> active = new Queue<BulletHoleDecal>();

    MeshRenderer meshRenderer;
    MaterialPropertyBlock mpb;
    float age;

    /// <summary>
    /// Create a bullet hole sitting on a surface.
    /// </summary>
    /// <param name="point">World impact point (use RaycastHit.point).</param>
    /// <param name="normal">Surface normal at the impact (use RaycastHit.normal).</param>
    /// <param name="surface">Optional transform to parent to, so the hole follows moving objects.</param>
    /// <param name="size">Hole diameter in meters.</param>
    public static BulletHoleDecal Spawn(Vector3 point, Vector3 normal, Transform surface = null,
                                   float size = 0.15f)
    {
        // Recycle the oldest hole once we hit the cap.
        while (active.Count >= MaxHoles)
        {
            BulletHoleDecal oldest = active.Dequeue();
            if (oldest != null) Destroy(oldest.gameObject);
        }

        GameObject go = new GameObject("BulletHole");
        // Lift very slightly off the surface to avoid z-fighting, and orient to the normal.
        go.transform.position = point + normal * 0.01f;
        go.transform.rotation = Quaternion.LookRotation(-normal);
        // Random roll so repeated holes don't look like clones.
        go.transform.Rotate(0f, 0f, Random.Range(0f, 360f), Space.Self);
        if (surface != null) go.transform.SetParent(surface, true);

        BulletHoleDecal hole = go.AddComponent<BulletHoleDecal>();
        hole.size = size;
        hole.Build();
        active.Enqueue(hole);
        return hole;
    }

    void Build()
    {
        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        mf.sharedMesh = QuadMesh();
        meshRenderer.sharedMaterial = SharedMaterial();
        meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        transform.localScale = Vector3.one * size;
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age < lifetime) return;

        float t = (age - lifetime) / fadeDuration;
        if (t >= 1f) { Destroy(gameObject); return; }

        // Fade out via a per-renderer property block so the shared material is untouched.
        meshRenderer.GetPropertyBlock(mpb);
        mpb.SetColor("_Color", new Color(1f, 1f, 1f, 1f - t));
        meshRenderer.SetPropertyBlock(mpb);
    }

    // ---- Shared, lazily-built assets ---------------------------------------

    static Mesh quad;
    static Material sharedMaterial;

    static Mesh QuadMesh()
    {
        if (quad != null) return quad;
        quad = new Mesh { name = "BulletHoleQuad" };
        quad.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f), new Vector3(0.5f,  0.5f, 0f),
        };
        quad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        quad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        quad.RecalculateNormals();
        return quad;
    }

    static Material SharedMaterial()
    {
        if (sharedMaterial != null) return sharedMaterial;
        // Sprites/Default is a reliable transparent, double-sided, unlit shader that
        // works under both the Built-in and URP pipelines.
        sharedMaterial = new Material(Shader.Find("Sprites/Default")) { name = "BulletHole (runtime)" };
        sharedMaterial.mainTexture = HoleTexture();
        return sharedMaterial;
    }

    static Texture2D HoleTexture()
    {
        const int res = 128;
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, true)
        {
            name = "BulletHole (runtime)",
            wrapMode = TextureWrapMode.Clamp
        };

        Vector2 center = new Vector2(res / 2f, res / 2f);
        float maxR = res / 2f;
        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / maxR;
                Color c;
                if (d < 0.32f)
                {
                    // Dark, solid inner hole.
                    c = new Color(0.03f, 0.03f, 0.03f, 1f);
                }
                else if (d < 1f)
                {
                    // Scorched ring fading outward, with noise for a cracked look.
                    float ring = 1f - Mathf.InverseLerp(0.32f, 1f, d);
                    float noise = Mathf.PerlinNoise(x * 0.18f, y * 0.18f);
                    float a = ring * Mathf.Clamp01(0.45f + 0.55f * noise);
                    c = new Color(0.12f, 0.1f, 0.09f, a);
                }
                else
                {
                    c = Color.clear;
                }
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply();
        return tex;
    }
}
