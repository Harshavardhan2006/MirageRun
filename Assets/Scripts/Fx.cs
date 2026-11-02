using UnityEngine;

public static class Fx
{
    public static Material ParticleMaterial;

    public static void Burst(Vector3 position, Color color)
    {
        GameObject go = new GameObject("PickupFx");
        go.transform.position = position;
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 4.5f;
        main.startSize = 0.18f;
        main.startColor = color;
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 16) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        if (ParticleMaterial != null) renderer.sharedMaterial = ParticleMaterial;

        ps.Play();
        Object.Destroy(go, 1.5f);
    }
}
