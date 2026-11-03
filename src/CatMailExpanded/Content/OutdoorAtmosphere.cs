using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The game's volumetric fog is tuned for short indoor sightlines; looking across open water from the yard it
    /// piles up into a blinding white wall. While the camera is in (or looking out towards) the yard we smoothly
    /// thin the fog, and restore the profile's original values exactly once back inside.
    /// </summary>
    internal static class OutdoorAtmosphere
    {
        sealed class Original { public float Density, Distance, Scattering; }

        static readonly Dictionary<IntPtr, Original> _originals = new();
        static readonly List<VolumetricFogVolumeComponent> _components = new();
        static float _blend;          // 0 = indoor fog, 1 = outdoor fog
        static float _nextScan;

        const float OutdoorDensityScale = 0.03f;   // just a whisper of sea haze
        const float OutdoorScatterScale = 0.25f;   // much less sun glare through what remains
        const float OutdoorDistanceScale = 0.45f;

        static void Scan()
        {
            _components.Clear();
            foreach (var v in UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Volume>(FindObjectsSortMode.None))
            {
                var prof = v.sharedProfile;
                if (!prof) continue;
                foreach (var c in prof.components)
                {
                    var fog = c.TryCast<VolumetricFogVolumeComponent>();
                    if (fog == null) continue;
                    if (!_originals.ContainsKey(fog.Pointer))
                        _originals[fog.Pointer] = new Original { Density = fog.density.value, Distance = fog.distance.value, Scattering = fog.scattering.value };
                    _components.Add(fog);
                }
            }
        }

        static float DistanceToYard(Vector3 p)
        {
            var y = Zones.Yard;
            float dx = Mathf.Max(y.Min.x - p.x, 0, p.x - y.Max.x);
            float dz = Mathf.Max(y.Min.z - p.z, 0, p.z - y.Max.z);
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        public static void Update()
        {
            if (!ModProgress.YardUnlocked && _blend <= 0f) return;
            if (Time.unscaledTime >= _nextScan) { _nextScan = Time.unscaledTime + 2f; Scan(); }
            if (_components.Count == 0) return;

            var cam = Core.Cameras.View;
            float target = 0f;
            if (cam && ModProgress.YardUnlocked)
                target = Mathf.Clamp01(1f - DistanceToYard(cam.transform.position) / 2.5f);
            _blend = Mathf.MoveTowards(_blend, target, Time.unscaledDeltaTime * 1.2f);
            Apply(_blend);
        }

        static void Apply(float t)
        {
            float k = t * t * (3f - 2f * t);
            foreach (var fog in _components)
            {
                if (fog == null || !_originals.TryGetValue(fog.Pointer, out var o)) continue;
                fog.density.value = Mathf.Lerp(o.Density, o.Density * OutdoorDensityScale, k);
                fog.distance.value = Mathf.Lerp(o.Distance, o.Distance * OutdoorDistanceScale, k);
                fog.scattering.value = Mathf.Lerp(o.Scattering, o.Scattering * OutdoorScatterScale, k);
            }
        }

        public static string Describe()
        {
            var sb = new System.Text.StringBuilder($"blend={_blend:0.00} components={_components.Count}\n");
            foreach (var f in _components)
                sb.AppendLine($"  density={f.density.value:0.###} dist={f.distance.value:0.#} scatter={f.scattering.value:0.###} aniso={f.anisotropy.value:0.##} mainLight={f.enableMainLightContribution.value} addLights={f.enableAdditionalLightsContribution.value} tint={f.tint.value} atten={f.attenuationDistance.value:0.#} maxH={f.maximumHeight.value:0.#}");
            return sb.ToString();
        }

        /// <summary>Restores the profiles (they are shared assets that outlive the scene).</summary>
        public static void Reset()
        {
            try { Apply(0f); } catch { }
            _blend = 0f;
            _components.Clear();
        }
    }
}
