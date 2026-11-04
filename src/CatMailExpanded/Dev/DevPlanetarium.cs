using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static CatMailExpanded.Dev.DevConsole;

namespace CatMailExpanded.Dev
{
    /// <summary>Developer commands for the planetarium, the otherworldly parcels and the front path.</summary>
    internal static class DevPlanetarium
    {
        static string B(Bounds b) => FormattableString.Invariant($"({b.min.x:0.00},{b.min.y:0.00},{b.min.z:0.00})..({b.max.x:0.00},{b.max.y:0.00},{b.max.z:0.00})");

        public static void Register()
        {
            DevConsole.Register("rooms", a =>
            {
                // every StorageRoom: safe constraints, its colliders, doors
                var sb = new StringBuilder();
                foreach (var r in Resources.FindObjectsOfTypeAll<Il2Cpp.StorageRoom>())
                {
                    if (!r || !r.gameObject.scene.IsValid()) continue;
                    sb.AppendLine($"{PathOf(r.transform)} safe={(int)r.SafeConstraints} ({r.SafeConstraints}) doors={(r.LinkedDoors != null ? r.LinkedDoors.Length : -1)} unlockable={(r.Unlockable != null)} layer={r.gameObject.layer} enabled={r.enabled}");
                    foreach (var c in r.GetComponentsInChildren<Collider>(true))
                        sb.AppendLine($"   {PathOf(c.transform)} {c.GetIl2CppType().Name} trig={c.isTrigger} layer={c.gameObject.layer} {B(c.bounds)}");
                }
                Out(sb.ToString());
            });
            DevConsole.Register("parcelrooms", a =>
            {
                var sb = new StringBuilder();
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    var pr = p.Properties;
                    if (pr == null) continue;
                    if (a.Length > 1 && ((int)pr.StorageConstraint).ToString() != a[1] && pr.StorageConstraint.ToString() != a[1]) continue;
                    var rooms = new StringBuilder();
                    try { foreach (var r in pr._storageRoomsIn) rooms.Append(r.name).Append(','); } catch (Exception e) { rooms.Append(e.Message); }
                    sb.AppendLine($"{p.name} c={(int)pr.StorageConstraint} dmg={pr.IsDamaged} pos={p.transform.position} rooms=[{rooms}] layer={p.gameObject.layer}");
                }
                Out(sb.ToString());
            });
            DevConsole.Register("checkdamage", a =>
            {
                int n = 0;
                foreach (var p in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityParcel>(FindObjectsSortMode.None))
                {
                    var pr = p.Properties;
                    if (pr == null || (a.Length > 1 && ((int)pr.StorageConstraint).ToString() != a[1])) continue;
                    pr.CheckForParcelDamage();
                    n++;
                }
                Out("checked " + n);
            });
            DevConsole.Register("mats", a =>
            {
                // mats <renderer path contains>: shader properties of the first match
                foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    if (!PathOf(r.transform).Contains(a[1])) continue;
                    var sb = new StringBuilder(PathOf(r.transform) + "\n");
                    foreach (var m in r.sharedMaterials)
                    {
                        if (!m) continue;
                        sb.AppendLine($" mat {m.name} shader={m.shader.name} queue={m.renderQueue} keywords={string.Join(",", (string[])m.shaderKeywords)}");
                        var sh = m.shader;
                        for (int i = 0; i < sh.GetPropertyCount(); i++)
                        {
                            var pn = sh.GetPropertyName(i);
                            var t = sh.GetPropertyType(i);
                            string v = t switch
                            {
                                UnityEngine.Rendering.ShaderPropertyType.Texture => m.GetTexture(pn) ? m.GetTexture(pn).name : "-",
                                UnityEngine.Rendering.ShaderPropertyType.Color => m.GetColor(pn).ToString(),
                                UnityEngine.Rendering.ShaderPropertyType.Vector => m.GetVector(pn).ToString(),
                                _ => m.GetFloat(pn).ToString(System.Globalization.CultureInfo.InvariantCulture),
                            };
                            sb.AppendLine($"   {pn} {t} = {v}");
                        }
                    }
                    Out(sb.ToString());
                    return;
                }
                Out("no renderer " + a[1]);
            });
            DevConsole.Register("meshread", a =>
            {
                var go = Core.World.Find(a[1]);
                var mf = go ? go.GetComponent<MeshFilter>() : null;
                if (!mf) { Out("no mesh"); return; }
                var m = mf.sharedMesh;
                Out($"{m.name} readable={m.isReadable} verts={m.vertexCount} sub={m.subMeshCount} bounds={m.bounds.center}/{m.bounds.size} pos={go.transform.position} rot={go.transform.eulerAngles} scale={go.transform.lossyScale}");
            });
            DevConsole.Register("labels", a =>
            {
                var sb = new StringBuilder();
                foreach (var l in UnityEngine.Object.FindObjectsByType<Il2Cpp.EntityStorageLabel>(FindObjectsSortMode.None))
                {
                    var pid = l.GetComponent<Il2Cpp.EntityPermanentIdentifier>();
                    sb.AppendLine($"{PathOf(l.transform)} id={(pid ? pid.PermanentIdentifier : -1)} idx={l._currentSpriteIndex} start={l.StartingSpriteIndex} pos={l.transform.position}");
                }
                Out(sb.ToString());
            });
            DevConsole.Register("planetarium", a => Out(Content.Planetarium.Describe()));
            DevConsole.Register("glowmasks", a => Out("wrote " + Content.BlackLight.DumpMasks(System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "CME"))));
            DevConsole.Register("shaderlist", a =>
            {
                var sb = new StringBuilder();
                foreach (var s in Resources.FindObjectsOfTypeAll<Shader>())
                    if (a.Length < 2 || s.name.Contains(a[1], StringComparison.OrdinalIgnoreCase)) sb.AppendLine(s.name);
                Out(sb.ToString());
            });
            DevConsole.Register("verts", a =>
            {
                // verts x0 y0 z0 x1 y1 z1 : distinct x / z of readable mesh vertices inside the box, per renderer
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                var lo = new Vector3(F(1), F(2), F(3));
                var hi = new Vector3(F(4), F(5), F(6));
                var sb = new StringBuilder();
                foreach (var mf in UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
                {
                    var m = mf.sharedMesh;
                    var r = mf.GetComponent<MeshRenderer>();
                    if (!m || !r || !r.enabled) continue;
                    var b = r.bounds;
                    if (b.max.x < lo.x || b.min.x > hi.x || b.max.y < lo.y || b.min.y > hi.y || b.max.z < lo.z || b.min.z > hi.z) continue;
                    if (!m.isReadable) { sb.AppendLine(PathOf(mf.transform) + " (not readable)"); continue; }
                    var xs = new SortedSet<float>();
                    var zs = new SortedSet<float>();
                    var l2w = mf.transform.localToWorldMatrix;
                    foreach (var v in m.vertices)
                    {
                        var w = l2w.MultiplyPoint3x4(v);
                        if (w.x < lo.x || w.x > hi.x || w.y < lo.y || w.y > hi.y || w.z < lo.z || w.z > hi.z) continue;
                        xs.Add(Mathf.Round(w.x * 100) / 100);
                        zs.Add(Mathf.Round(w.z * 100) / 100);
                    }
                    if (xs.Count == 0) continue;
                    sb.AppendLine(FormattableString.Invariant($"{PathOf(mf.transform)} x=[{string.Join(" ", xs)}] z=[{string.Join(" ", zs)}]"));
                }
                Out(sb.ToString());
            });
            DevConsole.Register("matcolor", a =>
            {
                // matcolor <material name part> <property> r g b : sets a colour on every loaded material matching
                float F(int i) => float.Parse(a[i], System.Globalization.CultureInfo.InvariantCulture);
                int n = 0;
                foreach (var m in Resources.FindObjectsOfTypeAll<Material>())
                    if (m && m.name.Contains(a[1], StringComparison.OrdinalIgnoreCase) && m.HasProperty(a[2])) { m.SetColor(a[2], new Color(F(3), F(4), F(5), 1f)); n++; }
                Out($"set {n}");
            });
            DevConsole.Register("overlaps", a =>
            {
                // overlaps <root>: pairs of the root's child objects whose bounds intersect, and how many vertices of
                // the root's big merged meshes sit inside each smaller child (e.g. a modelled prop inside a shelf)
                var root = GameObject.Find(a[1]);
                if (!root) { Out("no root"); return; }
                float shrink = a.Length > 2 ? float.Parse(a[2], System.Globalization.CultureInfo.InvariantCulture) : 0.03f;
                var kids = new List<(string name, Bounds b)>();
                var big = new List<MeshFilter>();
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    var c = root.transform.GetChild(i);
                    if (!c.gameObject.activeInHierarchy) continue;
                    var rs = c.GetComponentsInChildren<MeshRenderer>();
                    if (rs.Length == 0) continue;
                    var b = rs[0].bounds;
                    foreach (var r in rs) b.Encapsulate(r.bounds);
                    if (b.size.x * b.size.y * b.size.z > 15f) { var mf = c.GetComponent<MeshFilter>(); if (mf && mf.sharedMesh && mf.sharedMesh.isReadable) big.Add(mf); continue; }
                    kids.Add((c.name + "@" + FormattableString.Invariant($"{b.center.x:0.00},{b.center.z:0.00}"), b));
                }
                var sb = new StringBuilder();
                for (int i = 0; i < kids.Count; i++)
                    for (int j = i + 1; j < kids.Count; j++)
                    {
                        var p = kids[i].b; var q = kids[j].b;
                        var mn = Vector3.Max(p.min, q.min); var mx = Vector3.Min(p.max, q.max);
                        var d = mx - mn;
                        if (d.x > shrink && d.y > shrink && d.z > shrink)
                            sb.AppendLine(FormattableString.Invariant($"bounds {kids[i].name} x {kids[j].name} overlap=({d.x:0.00},{d.y:0.00},{d.z:0.00})"));
                    }
                foreach (var mf in big)
                {
                    var verts = mf.sharedMesh.vertices;
                    var l2w = mf.transform.localToWorldMatrix;
                    foreach (var k in kids)
                    {
                        var b = k.b; b.Expand(-2 * shrink);
                        int n = 0;
                        foreach (var v in verts) { var w = l2w.MultiplyPoint3x4(v); if (w.x > b.min.x && w.x < b.max.x && w.y > b.min.y && w.y < b.max.y && w.z > b.min.z && w.z < b.max.z) n++; }
                        if (n > 0) sb.AppendLine($"inside {mf.name} -> {k.name}: {n} verts");
                    }
                }
                Out(sb.Length > 0 ? sb.ToString() : "none");
            });
            DevConsole.Register("dumptex", a =>
            {
                // dumptex <name part>: writes every loaded texture whose name contains it to UserData/CME/tex_<name>.png
                var dir = System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "CME");
                var sb = new StringBuilder();
                foreach (var tex in Resources.FindObjectsOfTypeAll<Texture2D>())
                {
                    if (!tex || !tex.name.Contains(a[1], StringComparison.OrdinalIgnoreCase)) continue;
                    var rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    Graphics.Blit(tex, rt);
                    var prev = RenderTexture.active;
                    RenderTexture.active = rt;
                    var copy = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                    copy.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                    copy.Apply();
                    RenderTexture.active = prev;
                    RenderTexture.ReleaseTemporary(rt);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, "tex_" + tex.name + ".png"), ImageConversion.EncodeToPNG(copy));
                    UnityEngine.Object.Destroy(copy);
                    sb.AppendLine($"{tex.name} {tex.width}x{tex.height}");
                }
                Out(sb.ToString());
            });
        }
    }
}
