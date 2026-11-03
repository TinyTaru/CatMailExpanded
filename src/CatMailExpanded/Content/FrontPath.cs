using System;
using System.Collections.Generic;
using System.Globalization;
using CatMailExpanded.Core;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The yard's south-east corner leads to the front of the building: a closed garden gate in the deck railing,
    /// a footbridge over the channel, a signpost and a flagstone path along the building's south side, lit by two
    /// lamp posts at night. Purely scenery: the yard's tall invisible railing keeps everyone (and every parcel) on
    /// the deck, so the gate never opens.
    /// </summary>
    internal static class FrontPath
    {
        static GameObject _root;
        static readonly List<MeshRenderer> _bulbs = new();
        static Material _bulbMat;
        static float _lastNight = -1f;

        public static void Reset()
        {
            _root = null;
            _bulbs.Clear();
            _bulbMat = null;
            _lastNight = -1f;
        }

        public static void Build()
        {
            if (_root) return;
            try { BuildInternal(); }
            catch (Exception e) { Plugin.Log.Error("Front path build failed: " + e); }
        }

        static void BuildInternal()
        {
            _root = World.Root("CME_FrontPath");
            var t = _root.transform;
            World.Mesh("front_gate", "tex_front_gate", t, Vector3.zero, 0);
            World.Mesh("front_bridge", "tex_front_bridge", t, Vector3.zero, 0);
            var path = World.Mesh("front_path", "tex_front_path", t, Vector3.zero, 0);
            var pr = path.GetComponent<MeshRenderer>();
            pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            _bulbMat = ModAssets.UnlitMaterial("front_bulb", null, new Color(1f, 0.85f, 0.55f));
            var lamps = ModAssets.Text("front_lamps.txt");
            if (lamps != null)
                foreach (var raw in lamps.Split('\n'))
                {
                    var p = raw.Trim().Split(' ');
                    if (p.Length < 3) continue;
                    var pos = new Vector3(float.Parse(p[0], CultureInfo.InvariantCulture), float.Parse(p[1], CultureInfo.InvariantCulture),
                        float.Parse(p[2], CultureInfo.InvariantCulture));
                    var bulb = Shapes.Make("CME_LampBulb", Shapes.Sphere(16, 8), _bulbMat, t, pos, Quaternion.identity, Vector3.one * 0.075f);
                    var r = bulb.GetComponent<MeshRenderer>();
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    _bulbs.Add(r);
                }
            Plugin.Log.Msg("Front path built");
        }

        public static void Update()
        {
            if (!_root || !_bulbMat) return;
            float night = Lighting.NightFactor();
            if (Mathf.Abs(night - _lastNight) < 0.01f) return;
            _lastNight = night;
            // a warm glow after dark, dull glass by day
            _bulbMat.SetColor("_BaseColor", Color.Lerp(new Color(0.75f, 0.68f, 0.52f), new Color(2.2f, 1.7f, 0.95f), night));
        }
    }
}
