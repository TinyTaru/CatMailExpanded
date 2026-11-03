using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;
using Random = UnityEngine.Random;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// Spectral parcels fade from sight anywhere but the mirror room: their box and ribbon flicker out and only
    /// their stamps, stickers and name label stay visible (the highlight outline still shows when you aim at one).
    /// Brought into the mirror room they flicker back in. Hidden bodies are moved to a layer the player's camera
    /// skips but the mirror room's mirrors render, so a mirror shows every spectral parcel in front of it.
    /// Instead of the storage damage other kinds get, this is their whole consequence.
    /// </summary>
    internal static class Spectral
    {
        public const int HiddenLayer = 31;
        public static int HiddenLayerMask => 1 << HiddenLayer;
        // the label graphics of spectral parcels: drawn by the player's camera, never by a mirror's
        public const int UnreflectedLayer = 30;
        public static int MirrorExcludedMask => (1 << HiddenLayer) | (1 << UnreflectedLayer);
        const float FadeOutDelay = 1.5f;       // seconds outside the room before it starts to fade
        const float FadeSpeed = 0.9f;          // visibility per second

        public sealed class State
        {
            public float Visibility = 1f;
            public float OutsideSince = -1f;
            public bool Inside;
            public readonly List<(GameObject go, int layer)> Parts = new();
            public bool Hidden;                 // parts currently on the hidden layer
            public int Signature;
            // everything drawn for the parcel (stickers, outline and labels too): switched off for mirror cameras
            public readonly List<Renderer> Drawn = new();
            public readonly List<Canvas> Canvases = new();
            public readonly List<(GameObject go, int layer)> LabelParts = new();
            public readonly List<GameObject> AddedCanvases = new();
            public UnityEngine.Rendering.DisallowGPUDrivenRendering Classic;
            public int ClassicCount = -1;
        }

        /// <summary>Spectral parcels being tracked: they never show in a mirror.</summary>
        static readonly HashSet<State> _active = new();

        /// <summary>Keeps the hidden layer out of whatever the player is looking through (and the unreflected layer in).</summary>
        public static void Update()
        {
            int mask = HiddenLayerMask;
            var cam = Cameras.View;
            int seen = 1 << UnreflectedLayer;
            if (cam && ((cam.cullingMask & mask) != 0 || (cam.cullingMask & seen) == 0)) cam.cullingMask = (cam.cullingMask & ~mask) | seen;
            var main = Camera.main;
            if (main && main != cam && ((main.cullingMask & mask) != 0 || (main.cullingMask & seen) == 0)) main.cullingMask = (main.cullingMask & ~mask) | seen;
        }

        static void Collect(EntityParcel p, State s)
        {
            var visuals = p.transform.Find("Visuals");
            if (!visuals) return;
            var rs = visuals.GetComponentsInChildren<MeshRenderer>(true);
            int sig = rs.Length;
            foreach (var r in rs) sig = sig * 31 + r.gameObject.GetInstanceID();
            if (sig == s.Signature) return;
            if (s.Hidden) SetHidden(s, false);
            s.Signature = sig;
            s.Parts.Clear();
            foreach (var r in rs)
            {
                if (!r) continue;
                var n = r.gameObject.name;
                // keep the highlight outline (aiming at a hidden parcel still shows where it is) and our overlays
                if (n.Contains("Outline") || n.StartsWith("CME_")) continue;
                if (r.GetComponent<Collider>()) continue;   // never move a collider to another layer
                s.Parts.Add((r.gameObject, r.gameObject.layer));
            }
        }

        static void SetHidden(State s, bool hidden)
        {
            if (s.Hidden == hidden) return;
            s.Hidden = hidden;
            foreach (var (go, layer) in s.Parts)
                if (go) go.layer = hidden ? HiddenLayer : layer;
        }

        /// <summary>Every logic tick, for each tracked parcel.</summary>
        public static void Logic(EntityParcel p, State s, bool spectral, float now)
        {
            if (!spectral)
            {
                if (s.Hidden || s.Visibility < 1f) Release(s);
                return;
            }
            Collect(p, s);
            s.Drawn.Clear();
            foreach (var r in p.GetComponentsInChildren<Renderer>(true)) if (r) s.Drawn.Add(r);
            s.Canvases.Clear();
            foreach (var c in p.GetComponentsInChildren<Canvas>(true)) if (c) s.Canvases.Add(c);
            // a canvas can't be switched per camera either, and it is culled by its own layer: each label's graphics
            // get a nested canvas of their own on a layer the mirrors don't draw (the label's canvas keeps its
            // collider on the stamp layer)
            foreach (var c in s.Canvases)
            {
                if (!c || c.GetComponent<Collider>() == null) continue;
                for (int i = 0; i < c.transform.childCount; i++)
                {
                    var go = c.transform.GetChild(i).gameObject;
                    if (go.layer == UnreflectedLayer) continue;
                    if (!go.GetComponent<Canvas>()) { go.AddComponent<Canvas>(); s.AddedCanvases.Add(go); }
                    s.LabelParts.Add((go, go.layer));
                    go.layer = UnreflectedLayer;
                }
            }
            KeepClassic(p, s);
            _active.Add(s);
            s.Inside = MirrorRoom.Contains(p.transform.position);
            if (s.Inside) s.OutsideSince = -1f;
            else if (s.OutsideSince < 0f) s.OutsideSince = now;
        }

        /// <summary>Every frame: fades towards visible in the mirror room, invisible elsewhere, with a ghostly flicker.</summary>
        public static void Animate(State s, bool spectral, float dt, float now)
        {
            if (!spectral || s.Parts.Count == 0) return;
            bool wantVisible = s.Inside || (s.OutsideSince >= 0f && now - s.OutsideSince < FadeOutDelay);
            float target = wantVisible ? 1f : 0f;
            if (s.Visibility == target)
            {
                SetHidden(s, target == 0f);
                return;
            }
            s.Visibility = Mathf.MoveTowards(s.Visibility, target, dt * FadeSpeed);
            // while fading, the body flickers: shown with a probability that follows the fade
            bool show = s.Visibility >= 1f || (s.Visibility > 0f && Random.value < s.Visibility * s.Visibility);
            SetHidden(s, !show);
        }

        /// <summary>
        /// Called as each camera starts rendering: for a mirror camera every spectral parcel is switched off (the
        /// body, its stickers and its label), for any other camera switched back on.
        /// </summary>
        /// <summary>
        /// The GPU resident drawer only takes a renderer's on/off state once a frame, so switching it per camera does
        /// nothing there: a spectral parcel is drawn the classic way instead (re-applied when it gains renderers).
        /// </summary>
        static void KeepClassic(EntityParcel p, State s)
        {
            try
            {
                if (!s.Classic)
                {
                    s.Classic = p.gameObject.GetComponent<UnityEngine.Rendering.DisallowGPUDrivenRendering>()
                        ?? p.gameObject.AddComponent<UnityEngine.Rendering.DisallowGPUDrivenRendering>();
                    s.ClassicCount = -1;
                }
                if (s.ClassicCount == s.Drawn.Count) return;
                s.ClassicCount = s.Drawn.Count;
                s.Classic.enabled = false;
                s.Classic.applyToChildrenRecursively = true;
                s.Classic.enabled = true;
            }
            catch (Exception e) { Plugin.Log.Warning("Spectral classic rendering: " + e.Message); s.ClassicCount = s.Drawn.Count; }
        }

        public static int MirrorPasses;
        public static string Describe()
        {
            int drawn = 0, canv = 0, off = 0;
            foreach (var s in _active) { drawn += s.Drawn.Count; canv += s.Canvases.Count; foreach (var r in s.Drawn) if (r && r.forceRenderingOff) off++; }
            return $"spectral active={_active.Count} drawn={drawn} canvases={canv} off-now={off} mirrorPasses={MirrorPasses}";
        }

        public static bool DebugAlwaysOff;

        public static void MirrorPass(bool mirror)
        {
            if (mirror) MirrorPasses++;
            mirror |= DebugAlwaysOff;
            foreach (var s in _active)
                foreach (var r in s.Drawn) if (r && r.forceRenderingOff != mirror) r.forceRenderingOff = mirror;
        }

        public static void Release(State s)
        {
            if (s == null) return;
            if (_active.Remove(s))
            {
                foreach (var r in s.Drawn) if (r) r.forceRenderingOff = false;
            }
            foreach (var (go, layer) in s.LabelParts) if (go) go.layer = layer;
            s.LabelParts.Clear();
            foreach (var go in s.AddedCanvases) if (go && go.GetComponent<Canvas>()) UnityEngine.Object.Destroy(go.GetComponent<Canvas>());
            s.AddedCanvases.Clear();
            s.Drawn.Clear();
            s.Canvases.Clear();
            if (s.Classic) UnityEngine.Object.Destroy(s.Classic);
            s.Classic = null;
            s.ClassicCount = -1;
            SetHidden(s, false);
            s.Visibility = 1f;
            s.OutsideSince = -1f;
            s.Signature = 0;
            s.Parts.Clear();
        }
    }
}
