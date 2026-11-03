using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>
    /// Loads the mod's embedded art (meshes exported from Blender as .cmemesh, PNG textures)
    /// and builds materials that use the game's own shaders so new props match the art style.
    /// </summary>
    internal static class ModAssets
    {
        static readonly Dictionary<string, Mesh> _meshes = new();
        static readonly Dictionary<string, Texture2D> _textures = new();
        static readonly Dictionary<string, Material> _materials = new();
        static Material _propTemplate;

        static byte[] ReadResource(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream("CME/" + name);
            if (s == null) throw new FileNotFoundException("Embedded asset missing: " + name);
            using var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }

        public static byte[] Bytes(string name) => ReadResource(name);

        /// <summary>An embedded text file (e.g. a placement layout), or null when missing.</summary>
        public static string Text(string name)
        {
            try { return System.Text.Encoding.UTF8.GetString(ReadResource(name)); }
            catch (FileNotFoundException) { return null; }
        }

        public static Texture2D Texture(string name, bool linear = false)
        {
            if (_textures.TryGetValue(name, out var t) && t) return t;
            var bytes = ReadResource(name + ".png");
            var rgba = PngDecoder.Decode(bytes, out int w, out int h);
            t = new Texture2D(w, h, TextureFormat.RGBA32, true, linear);
            unsafe
            {
                fixed (byte* p = rgba)
                    t.SetPixelDataImpl((IntPtr)p, 0, 1, rgba.Length, 0);
            }
            t.Apply(true, false);
            t.name = "CME_" + name;
            t.wrapMode = TextureWrapMode.Clamp;
            t.anisoLevel = 4;
            t.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _textures[name] = t;
            return t;
        }

        public static Mesh Mesh(string name)
        {
            if (_meshes.TryGetValue(name, out var m) && m) return m;
            var bytes = ReadResource(name + ".cmemesh");
            using var br = new BinaryReader(new MemoryStream(bytes));
            if (new string(br.ReadChars(4)) != "CMEM") throw new InvalidDataException("bad mesh " + name);
            br.ReadUInt32(); // version
            int subCount = (int)br.ReadUInt32();
            int vCount = (int)br.ReadUInt32();
            var pos = new Vector3[vCount];
            var nrm = new Vector3[vCount];
            var uv = new Vector2[vCount];
            var col = new Color[vCount];
            for (int i = 0; i < vCount; i++)
            {
                pos[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                nrm[i] = new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
                uv[i] = new Vector2(br.ReadSingle(), br.ReadSingle());
                col[i] = new Color(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            }
            m = new Mesh { name = "CME_" + name };
            if (vCount > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = pos;
            m.normals = nrm;
            m.uv = uv;
            m.colors = col;
            m.subMeshCount = subCount;
            for (int s = 0; s < subCount; s++)
            {
                int nl = (int)br.ReadUInt32();
                br.ReadBytes(nl);
                int ic = (int)br.ReadUInt32();
                var idx = new int[ic];
                for (int i = 0; i < ic; i++) idx[i] = (int)br.ReadUInt32();
                m.SetTriangles(idx, s);
            }
            m.RecalculateBounds();
            m.RecalculateTangents();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _meshes[name] = m;
            return m;
        }

        /// <summary>A material using the game's prop shader (sh_MasterShader) with our baked texture.</summary>
        public static Material PropMaterial(string textureName)
        {
            if (_materials.TryGetValue(textureName, out var mat) && mat) return mat;
            var template = PropTemplate();
            mat = template ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.name = "CME_" + textureName;
            var tex = Texture(textureName);
            tex.wrapMode = TextureWrapMode.Repeat;
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", tex);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", tex);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            mat.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _materials[textureName] = mat;
            return mat;
        }

        /// <summary>Finds a material from a static break room prop to copy shader settings from.</summary>
        public static Material PropTemplate()
        {
            if (_propTemplate) return _propTemplate;
            foreach (var r in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var m = r.sharedMaterial;
                if (!m || m.shader == null) continue;
                if (m.name == "m_Breakroom_Props" && m.shader.name == "Shader Graphs/sh_MasterShader")
                {
                    _propTemplate = m;
                    Plugin.Log.Msg($"Prop template material: {m.name} shader={m.shader.name}");
                    break;
                }
            }
            return _propTemplate;
        }

        /// <summary>Creates a static prop GameObject from a mesh + baked texture.</summary>
        public static GameObject Prop(string mesh, string texture, Transform parent, Vector3 pos, Quaternion rot, bool collider = true)
        {
            var go = new GameObject("CME_" + mesh);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = rot;
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = Mesh(mesh);
            var mr = go.AddComponent<MeshRenderer>();
            var mat = PropMaterial(texture);
            var mats = new Material[mf.sharedMesh.subMeshCount];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            mr.sharedMaterials = new Il2CppReferenceArray<Material>(mats);
            if (collider)
            {
                var bc = go.AddComponent<BoxCollider>();
                var b = mf.sharedMesh.bounds;
                bc.center = b.center;
                bc.size = b.size;
            }
            return go;
        }

        public static void ClearSceneCaches()
        {
            _propTemplate = null;
        }
    }
}
