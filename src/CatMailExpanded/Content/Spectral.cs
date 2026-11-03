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
        }

        /// <summary>Keeps the hidden layer out of whatever the player is looking through.</summary>
        public static void Update()
        {
            int mask = HiddenLayerMask;
            var cam = Cameras.View;
            if (cam && (cam.cullingMask & mask) != 0) cam.cullingMask &= ~mask;
            var main = Camera.main;
            if (main && main != cam && (main.cullingMask & mask) != 0) main.cullingMask &= ~mask;
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

        public static void Release(State s)
        {
            if (s == null) return;
            SetHidden(s, false);
            s.Visibility = 1f;
            s.OutsideSince = -1f;
            s.Signature = 0;
            s.Parts.Clear();
        }
    }
}
