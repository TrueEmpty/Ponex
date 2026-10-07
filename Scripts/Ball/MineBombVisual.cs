using UnityEngine;

/// <summary>Builds a cartoon bomb look: dark body sphere + fuse stick + optional spark particles.</summary>
public static class MineBombVisual
{
    public static void Apply(GameObject root, Material bodyMat, Material fuseMat, bool litFuse, float fuseScale = 1f)
    {
        if (root == null)
            return;

        Renderer rootRen = root.GetComponent<Renderer>();
        if (rootRen != null && bodyMat != null)
            rootRen.sharedMaterial = bodyMat;

        Transform existing = root.transform.Find("BombFuse");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        GameObject fuse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        fuse.name = "BombFuse";
        fuse.transform.SetParent(root.transform, false);
        fuse.transform.localPosition = new Vector3(0f, 0.55f * fuseScale, 0f);
        fuse.transform.localRotation = Quaternion.identity;
        fuse.transform.localScale = new Vector3(0.12f * fuseScale, 0.22f * fuseScale, 0.12f * fuseScale);
        Object.Destroy(fuse.GetComponent<Collider>());

        Renderer fuseRen = fuse.GetComponent<Renderer>();
        if (fuseRen != null)
            fuseRen.sharedMaterial = fuseMat != null ? fuseMat : bodyMat;

        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = "FuseTip";
        tip.transform.SetParent(fuse.transform, false);
        tip.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        tip.transform.localScale = Vector3.one * 0.55f;
        Object.Destroy(tip.GetComponent<Collider>());
        Renderer tipRen = tip.GetComponent<Renderer>();
        if (tipRen != null)
        {
            tipRen.sharedMaterial = fuseMat != null ? fuseMat : bodyMat;
            if (litFuse)
                tipRen.material.color = new Color(1f, 0.45f, 0.1f, 1f);
        }

        if (litFuse)
            AttachFuseSparks(tip.transform);
    }

    static void AttachFuseSparks(Transform tip)
    {
        GameObject fx = new GameObject("FuseSparks");
        fx.transform.SetParent(tip, false);
        fx.transform.localPosition = Vector3.zero;

        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.6f;
        main.startSize = 0.08f;
        main.startColor = new Color(1f, 0.55f, 0.15f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 40;
        main.gravityModifier = 0.4f;

        var emission = ps.emission;
        emission.rateOverTime = 28f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;

        var colorOver = ps.colorOverLifetime;
        colorOver.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f),
                new GradientColorKey(new Color(1f, 0.25f, 0.05f), 0.55f),
                new GradientColorKey(new Color(0.2f, 0.05f, 0.05f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.8f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOver.color = g;

        var renderer = fx.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        if (mat != null)
        {
            mat.SetColor("_Color", new Color(1f, 0.6f, 0.2f, 1f));
            renderer.sharedMaterial = mat;
        }
    }
}
