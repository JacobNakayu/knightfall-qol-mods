using Il2Cpp;

using MelonLoader;

using UnityEngine;

namespace TownBoundaryForceField;

internal static class ForceFieldManager
{
    private const string MaterialName = "M_VFX_PaintStroke";
    private const float RefreshInterval = 1f;

    private static readonly List<TownField> Fields = new();

    private static GameObject _root; // parent of every field; destroyed with the scene
    private static Material _material;
    private static float _nextRefreshTime;

    public static void Tick()
    {
        // The root goes away on scene change, taking every particle system with it
        if (_root == null && Fields.Count > 0) Fields.Clear();

        Player player = Player.localplayer;
        if (player == null) return;

        if (Time.time >= _nextRefreshTime)
        {
            _nextRefreshTime = Time.time + RefreshInterval;
            if (!EnsureFields()) return;
            foreach (TownField field in Fields) field.Refresh();
        }

        if (Fields.Count == 0) return;

        Vector3 playerPosition = player.transform.position;
        float deltaTime = Time.deltaTime;
        foreach (TownField field in Fields) field.Tick(playerPosition, deltaTime);
    }

    private static bool EnsureFields()
    {
        if (_root != null) return true;
        if (!EnsureMaterial()) return false;

        Farm[] farms = UnityEngine.Object.FindObjectsOfType<Farm>().ToArray();
        if (farms.Length == 0) return false;

        _root = new GameObject("TownBoundaryForceField");
        _root.transform.position = Vector3.zero;

        // Some towns show up more than once, so build one field per collider
        HashSet<int> seen = new();
        foreach (Farm farm in farms)
        {
            Collider area = farm.col;
            if (area == null || !seen.Add(area.GetInstanceID())) continue;

            Fields.Add(new TownField(area, _root.transform, _material));
        }

        if (Fields.Count == 0)
        {
            UnityEngine.Object.Destroy(_root);
            _root = null;
            return false;
        }

        MelonLogger.Msg($"Created {Fields.Count} town force fields.");
        return true;
    }

    private static bool EnsureMaterial()
    {
        if (_material != null) return true;

        // Slow (searches every loaded material), but only runs until the material is found
        foreach (Material material in Resources.FindObjectsOfTypeAll<Material>())
        {
            if (material.name != MaterialName) continue;

            _material = material;
            return true;
        }
        return false;
    }
}