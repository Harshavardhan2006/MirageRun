using UnityEngine;

public class PlayerDust : MonoBehaviour
{
    ParticleSystem ps;
    PlayerMovement player;

    void Awake()
    {
        player = GetComponent<PlayerMovement>();
        GameObject go = new GameObject("Dust");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, -0.25f);
        go.transform.localRotation = Quaternion.Euler(-15f, 180f, 0f);

        ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.maxParticles = 120;
        main.startLifetime = 0.75f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.0f);
        main.startColor = new Color(0.92f, 0.8f, 0.6f, 0.55f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.03f;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.25f;

        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.7f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = g;

        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.7f));

        ps.Play();
    }

    void Start()
    {
        ParticleSystemRenderer r = ps.GetComponent<ParticleSystemRenderer>();
        if (Fx.ParticleMaterial != null) r.sharedMaterial = Fx.ParticleMaterial;
    }

    void Update()
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        bool active = GameManager.IsPlaying && player != null && player.Grounded;
        float t = Mathf.Clamp01((GameManager.Speed - 11f) / 15f);
        emission.rateOverTime = active ? Mathf.Lerp(16f, 36f, t) : 0f;
    }
}
