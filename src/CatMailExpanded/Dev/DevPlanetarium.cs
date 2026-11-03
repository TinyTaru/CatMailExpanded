using System;
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
        }
    }
}
