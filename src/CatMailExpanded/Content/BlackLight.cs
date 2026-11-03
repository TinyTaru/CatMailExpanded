using System;
using System.Collections.Generic;
using CatMailExpanded.Core;
using Il2Cpp;
using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>
    /// The planetarium is lit like a black-light room. Every parcel inside it is darkened (through a property
    /// block on the game's own shader colours); otherworldly parcels additionally get an additive overlay that
    /// makes the white parts of their textures (labels, tape, sticker borders) fluoresce violet-white, with a
    /// little of the stickers' brighter colours glowing too.
    /// </summary>
    internal static class BlackLight
    {
        public sealed class State
        {
            public float Dark, Glow;          // current amounts 0..1
            public bool Inside;
            public readonly List<Renderer> Renderers = new();
            // the customer name label is world-space UI (an Image on a canvas), not a mesh
            public readonly List<UnityEngine.UI.Image> Labels = new();
            public readonly Dictionary<IntPtr, Color> LabelColours = new();
            public readonly Dictionary<IntPtr, (Material original, Material glow)> LabelMats = new();
            public readonly Dictionary<IntPtr, MeshRenderer> Overlays = new();   // source renderer -> overlay
            public int RendererSignature;
            public bool Darkened;
        }

        static readonly Dictionary<IntPtr, Material> _glowMats = new();   // per source texture
        static readonly Dictionary<IntPtr, bool> _ownBlock = new();
        static MaterialPropertyBlock _mpb;
        static readonly int DiffuseId = Shader.PropertyToID("_DiffuseColor");
        static readonly int Diffuse1Id = Shader.PropertyToID("_DiffuseColor_1");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        static readonly Color GlowTint = new(1.1f, 1.0f, 1.8f);
        static readonly Color DarkLit = new(0.30f, 0.27f, 0.48f);
        static readonly Color DarkShade = new(0.10f, 0.09f, 0.20f);

        /// <summary>Dev: writes every glow mask made so far to a folder.</summary>
        public static int DumpMasks(string dir)
        {
            int n = 0;
            foreach (var m in _glowMats.Values)
            {
                if (!m) continue;
                var tex = m.GetTexture("_BaseMap")?.TryCast<Texture2D>();
                if (!tex) continue;
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, tex.name + ".png"), ImageConversion.EncodeToPNG(tex));
                n++;
            }
            return n;
        }

        public static void Clear()
        {
            _glowMats.Clear();
            _ownBlock.Clear();
        }

        static bool Skip(Renderer r)
        {
            var n = r.gameObject.name;
            return n.Contains("Outline") || n.Contains("Preview") || n.StartsWith("CME_Glow") || n.Contains("FakeShadow");
        }

        /// <summary>Called every logic tick for each tracked parcel.</summary>
        public static void Logic(EntityParcel p, State s, bool otherworldly)
        {
            s.Inside = Zones.InPlanetarium(p.transform.position);
            if (!s.Inside && s.Dark <= 0f && s.Glow <= 0f) return;
            // refresh the renderer list when stickers are added or removed
            var rs = p.GetComponentsInChildren<MeshRenderer>(true);
            var imgs = p.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            int sig = rs.Length * 7 + imgs.Length;
            foreach (var r in rs) sig = sig * 31 + (r.sharedMaterial ? r.sharedMaterial.GetInstanceID() : 0);
            if (sig != s.RendererSignature)
            {
                s.RendererSignature = sig;
                s.Renderers.Clear();
                foreach (var r in rs) if (r && !Skip(r)) s.Renderers.Add(r);
                s.Labels.Clear();
                foreach (var img in imgs)
                    if (img && !World.PathOf(img.transform).Contains("canvas_Debug")) s.Labels.Add(img);
                if (otherworldly) RebuildOverlays(s);
                if (s.Dark > 0f) ApplyDark(s);
            }
        }

        /// <summary>Called every frame; fades darkness and glow towards their targets.</summary>
        public static void Animate(State s, bool otherworldly, float dt)
        {
            float targetDark = s.Inside ? 1f : 0f;
            float targetGlow = s.Inside && otherworldly ? 1f : 0f;
            if (s.Dark == targetDark && s.Glow == targetGlow && !(s.Glow > 0 && otherworldly)) return;
            s.Dark = Mathf.MoveTowards(s.Dark, targetDark, dt * 1.6f);
            s.Glow = Mathf.MoveTowards(s.Glow, targetGlow, dt * 1.2f);
            ApplyDark(s);
            ApplyGlow(s);
        }

        public static void Release(State s)
        {
            if (s == null) return;
            s.Dark = s.Glow = 0f;
            ApplyDark(s);
            foreach (var o in s.Overlays.Values) if (o) UnityEngine.Object.Destroy(o.gameObject);
            s.Overlays.Clear();
            foreach (var img in s.Labels)
                if (img && s.LabelMats.TryGetValue(img.Pointer, out var lm) && img.material == lm.glow) img.material = lm.original;
            s.LabelMats.Clear();
            s.RendererSignature = 0;
        }

        static void ApplyDark(State s)
        {
            _mpb ??= new MaterialPropertyBlock();
            foreach (var r in s.Renderers)
            {
                if (!r) continue;
                var key = r.Pointer;
                // never fight a renderer that already uses its own property block
                if (!s.Darkened && !_ownBlock.ContainsKey(key)) _ownBlock[key] = r.HasPropertyBlock();
                if (_ownBlock.TryGetValue(key, out var own) && own) continue;
                if (s.Dark <= 0f) { r.SetPropertyBlock(null); continue; }
                var m = r.sharedMaterial;
                if (!m || !m.HasProperty(DiffuseId)) continue;
                var lit = m.GetColor(DiffuseId);
                var shade = m.HasProperty(Diffuse1Id) ? m.GetColor(Diffuse1Id) : lit * 0.3f;
                _mpb.Clear();
                _mpb.SetColor(DiffuseId, Color.Lerp(lit, new Color(lit.r * DarkLit.r, lit.g * DarkLit.g, lit.b * DarkLit.b, lit.a), s.Dark));
                _mpb.SetColor(Diffuse1Id, Color.Lerp(shade, DarkShade, s.Dark));
                r.SetPropertyBlock(_mpb);
            }
            foreach (var img in s.Labels)
            {
                if (!img) continue;
                if (!s.LabelColours.TryGetValue(img.Pointer, out var orig)) s.LabelColours[img.Pointer] = orig = img.color;
                img.color = Color.Lerp(orig, new Color(orig.r * DarkLit.r, orig.g * DarkLit.g, orig.b * DarkLit.b, orig.a), s.Dark * (1f - s.Glow));
                if (s.Dark <= 0f) s.LabelColours.Remove(img.Pointer);
            }
            s.Darkened = s.Dark > 0f;
            if (!s.Darkened)
                foreach (var r in s.Renderers) if (r) _ownBlock.Remove(r.Pointer);
        }

        static void ApplyGlow(State s)
        {
            bool on = s.Glow > 0.005f;
            // a slow shimmer, as if the parcel were breathing starlight
            float k = s.Glow * (0.85f + 0.15f * Mathf.Sin(Time.time * 2.3f));
            _mpb ??= new MaterialPropertyBlock();
            // name labels: brighten the label paper itself (HDR tint on its UI material); the name text,
            // drawn on top of it, stays dark and readable
            foreach (var img in s.Labels)
            {
                if (!img) continue;
                s.LabelMats.TryGetValue(img.Pointer, out var lm);
                if (on && !lm.glow)
                {
                    lm.original = img.material;
                    lm.glow = new Material(img.materialForRendering) { name = "CME_LabelGlow" };
                    s.LabelMats[img.Pointer] = lm;
                }
                if (!lm.glow) continue;
                if (on)
                {
                    if (img.material != lm.glow) img.material = lm.glow;
                    var c = Color.Lerp(Color.white, new Color(1.7f, 1.6f, 2.2f), k);
                    lm.glow.SetColor("_Color", c);
                }
                else if (img.material == lm.glow) img.material = lm.original;
            }
            foreach (var o in s.Overlays.Values)
            {
                if (!o) continue;
                if (o.enabled != on) o.enabled = on;
                if (!on) continue;
                _mpb.Clear();
                _mpb.SetColor(BaseColorId, GlowTint * k);
                o.SetPropertyBlock(_mpb);
            }
        }

        static void RebuildOverlays(State s)
        {
            var keep = new HashSet<IntPtr>();
            foreach (var r in s.Renderers)
            {
                if (!r) continue;
                var mr = r.TryCast<MeshRenderer>();
                var mf = r.GetComponent<MeshFilter>();
                if (mr == null || !mf || !mf.sharedMesh) continue;
                keep.Add(r.Pointer);
                var mats = r.sharedMaterials;
                var glow = new Material[mats.Length];
                bool any = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    glow[i] = GlowMaterial(mats[i]);
                    any |= glow[i] != null;
                }
                if (!any) continue;
                if (!s.Overlays.TryGetValue(r.Pointer, out var overlay) || !overlay)
                {
                    var go = new GameObject("CME_Glow");
                    go.transform.SetParent(r.transform, false);
                    go.layer = r.gameObject.layer;
                    go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                    overlay = go.AddComponent<MeshRenderer>();
                    overlay.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    overlay.receiveShadows = false;
                    overlay.enabled = false;
                    s.Overlays[r.Pointer] = overlay;
                }
                for (int i = 0; i < glow.Length; i++) glow[i] ??= Transparent();
                overlay.sharedMaterials = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppReferenceArray<Material>(glow);
            }
            var stale = new List<IntPtr>();
            foreach (var kv in s.Overlays) if (!keep.Contains(kv.Key)) stale.Add(kv.Key);
            foreach (var k in stale)
            {
                if (s.Overlays[k]) UnityEngine.Object.Destroy(s.Overlays[k].gameObject);
                s.Overlays.Remove(k);
            }
        }

        static Material _transparent;
        static Material Transparent() => _transparent ? _transparent : (_transparent = ModAssets.UnlitMaterial("glow_none", null, Color.black, true));

        /// <summary>An additive material showing only the fluorescent parts of a material's texture.</summary>
        static Material GlowMaterial(Material source)
        {
            if (!source || !source.HasProperty("_BaseMap")) return null;
            return GlowFor(source.GetTexture("_BaseMap"), source);
        }

        static Material GlowFor(Texture tex, Material source = null)
        {
            if (!tex) return null;
            if (_glowMats.TryGetValue(tex.Pointer, out var m) && m) return m;
            try
            {
                var mask = Mask(tex);
                m = ModAssets.UnlitMaterial("Glow_" + tex.name, mask, GlowTint, true);
                if (source)
                {
                    m.mainTextureScale = source.mainTextureScale;
                    m.mainTextureOffset = source.mainTextureOffset;
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Warning($"Glow mask for {tex.name} failed: {e.Message}");
                m = null;
            }
            _glowMats[tex.Pointer] = m;
            return m;
        }

        /// <summary>
        /// Reads the (GPU-only) texture back through a render texture and keeps what a black light would light up:
        /// near-white, unsaturated pixels fully, bright saturated colours a little.
        /// </summary>
        static Texture2D Mask(Texture src)
        {
            float scale = Mathf.Min(1f, 1024f / Mathf.Max(src.width, src.height));
            int w = Mathf.Max(4, (int)(src.width * scale)), h = Mathf.Max(4, (int)(src.height * scale));
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var read = new Texture2D(w, h, TextureFormat.RGBA32, false);
            read.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            read.Apply(false);
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            // stickers fluoresce in their own colours too; on the box itself only the whites (labels, tape) do
            bool sticker = src.name.Contains("Stamp") || src.name.Contains("sticker") || src.name.Contains("Sticker");
            // the paper name labels are cream rather than pure white: accept warmer whites there
            float satCut = src.name.Contains("Parcel") || src.name.Contains("Stamp") ? 0.12f : 0.3f;
            float colourAmount = sticker ? 0.4f : 0.12f;
            var px = read.GetPixels32();
            int n = px.Length;
            var outPx = new Color32[n];
            for (int i = 0; i < n; i++)
            {
                var c = px[i];
                float r = c.r / 255f, g = c.g / 255f, b = c.b / 255f, a = c.a / 255f;
                float mx = Mathf.Max(r, Mathf.Max(g, b)), mn = Mathf.Min(r, Mathf.Min(g, b));
                float sat = mx > 0.001f ? (mx - mn) / mx : 0f;
                float white = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.8f, mn)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(satCut, satCut + 0.23f, sat)));
                float or, og, ob;
                if (sticker)
                {
                    // the whole sticker fluoresces evenly in a pale version of its colours, whites a little brighter
                    float wb = 0.7f * white;
                    or = (0.32f * Mathf.Lerp(r, 1f, 0.6f) + wb) * a;
                    og = (0.32f * Mathf.Lerp(g, 1f, 0.6f) + wb) * a;
                    ob = (0.32f * Mathf.Lerp(b, 1f, 0.6f) + wb) * a;
                }
                else
                {
                    float colour = colourAmount * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 0.9f, mx)) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.55f, sat));
                    or = (white + colour * r) * a; og = (white + colour * g) * a; ob = (white + colour * b) * a;
                }
                outPx[i] = new Color32((byte)(Mathf.Clamp01(or) * 255), (byte)(Mathf.Clamp01(og) * 255), (byte)(Mathf.Clamp01(ob) * 255), 255);
            }
            UnityEngine.Object.Destroy(read);
            var mask = new Texture2D(w, h, TextureFormat.RGBA32, true);
            mask.SetPixels32(new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Color32>(outPx));
            mask.Apply(true, false);
            mask.wrapMode = TextureWrapMode.Repeat;
            mask.name = "CME_GlowMask_" + src.name;
            mask.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return mask;
        }
    }
}
