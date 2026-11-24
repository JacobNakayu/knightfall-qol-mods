using UnityEngine;

namespace TownBoundaryForceField;

// The force field for a single town: one particle system that emits on the surface of the town's sphere
internal sealed class TownField
{
    private const int ParticlesPerSecond = 300; // keep ParticlesPerSecond x Lifetime under 1000 (the particle cap per system)
    private const float Lifetime = 3f;
    private const float Size = 1.5f;
    private const float BandMin = -0.2f; // emission height on the sphere: -1 bottom, 0 center, 1 top
    private const float BandMax = 1f;
    private const float EmitDistance = 150f; // only emit while the player is this close to the sphere's surface
    private static readonly Color32 ParticleColor = new(0, 255, 0, 100);

    // Managed RNG: avoids two interop calls per particle compared to UnityEngine.Random
    private static readonly System.Random Rng = new();

    private readonly Collider _area;
    private readonly ParticleSystem _system;

    private bool _open;
    private Vector3 _center;
    private float _radius;
    private float _emitBudget;

    public TownField(Collider area, Transform parent, Material material)
    {
        _area = area;

        GameObject go = new($"ForceField_{area.gameObject.name}");
        go.transform.SetParent(parent, false); // parent sits at the origin, so local space == world space

        _system = go.AddComponent<ParticleSystem>(); // also adds a ParticleSystemRenderer
        _system.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
        _system.Stop(false, ParticleSystemStopBehavior.StopEmitting); // no automatic emission; we place every particle
    }

    // Called on the refresh interval: picks up towns opening and closing
    public void Refresh()
    {
        _open = _area != null && _area.enabled;
        if (!_open) return; // existing particles fade out on their own

        // Read every refresh: a disabled collider reports zero-sized bounds, so the radius is only valid while open
        Bounds bounds = _area.bounds;
        _center = bounds.center;
        _radius = bounds.extents.x;
    }

    // Called every frame
    public void Tick(Vector3 playerPosition, float deltaTime)
    {
        if (!_open) return;

        float distanceToSurface = Mathf.Abs(Vector3.Distance(playerPosition, _center) - _radius);
        if (distanceToSurface > EmitDistance)
        {
            _emitBudget = 0f; // don't save up a burst for when the player arrives
            return;
        }

        // Emit a steady rate regardless of frame rate
        _emitBudget += ParticlesPerSecond * deltaTime;
        while (_emitBudget >= 1f)
        {
            _emitBudget -= 1f;
            EmitOne();
        }
    }

    private void EmitOne()
    {
        // A uniform height in [BandMin, BandMax] plus a uniform angle spreads particles
        // evenly over that band of the sphere's surface
        float y = Mathf.Lerp(BandMin, BandMax, (float)Rng.NextDouble());
        float ring = Mathf.Sqrt(1f - y * y);
        float angle = (float)(Rng.NextDouble() * 2.0 * Mathf.PI);
        Vector3 dir = new(ring * Mathf.Cos(angle), y, ring * Mathf.Sin(angle));

        Vector3 position = _center + dir * _radius;
        Vector3 drift = dir * 0.1f + Vector3.up * 0.2f; // slight outward and upward float
        _system.Emit(position, drift, Size, Lifetime, ParticleColor);
    }
}