using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;

namespace CatMailExpanded.Core
{
    /// <summary>
    /// Cuts box-shaped holes (doorways) out of the game's static meshes. The game's meshes are not CPU readable,
    /// so the vertex and index buffers are read back from the GPU, triangles overlapping the hole are clipped
    /// to the parts outside it, and a new readable mesh replaces the old one on that MeshFilter only.
    /// </summary>
    internal static class MeshCutter
    {
        // per-vertex record: pos 0-2, normal 3-5, tangent 6-9, uv0 10-11, uv1 12-13, colour 14-17, world pos 18-20
        const int P = 0, N = 3, T = 6, U0 = 10, U1 = 12, C = 14, W = 18, Size = 21;

        sealed class Source
        {
            public List<float[]> Verts = new();
            public List<List<int>> Subs = new();
            public bool HasN, HasT, HasU0, HasU1, HasC;
        }

        static int FormatSize(VertexAttributeFormat f) => f switch
        {
            VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
            VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16
                or VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
            _ => 1,
        };

        static float ReadComponent(byte[] d, int o, VertexAttributeFormat f) => f switch
        {
            VertexAttributeFormat.Float32 => BitConverter.ToSingle(d, o),
            VertexAttributeFormat.Float16 => (float)BitConverter.ToHalf(d, o),
            VertexAttributeFormat.UNorm8 => d[o] / 255f,
            VertexAttributeFormat.SNorm8 => Mathf.Max(-1f, (sbyte)d[o] / 127f),
            VertexAttributeFormat.UNorm16 => BitConverter.ToUInt16(d, o) / 65535f,
            VertexAttributeFormat.SNorm16 => Mathf.Max(-1f, BitConverter.ToInt16(d, o) / 32767f),
            VertexAttributeFormat.UInt8 => d[o],
            VertexAttributeFormat.SInt8 => (sbyte)d[o],
            VertexAttributeFormat.UInt16 => BitConverter.ToUInt16(d, o),
            VertexAttributeFormat.SInt16 => BitConverter.ToInt16(d, o),
            VertexAttributeFormat.UInt32 => BitConverter.ToUInt32(d, o),
            VertexAttributeFormat.SInt32 => BitConverter.ToInt32(d, o),
            _ => 0f,
        };

        static byte[] ReadBuffer(GraphicsBuffer gb)
        {
            if (gb == null) throw new InvalidOperationException("GPU buffer not available");
            int bytes = gb.count * gb.stride;
            var arr = new Il2CppStructArray<byte>(bytes);
            gb.GetData(new Il2CppSystem.Array(arr.Pointer), 0, 0, bytes);
            var managed = new byte[bytes];
            for (int i = 0; i < bytes; i++) managed[i] = arr[i];
            gb.Release();
            return managed;
        }

        static Source Read(Mesh mesh, Matrix4x4 toWorld)
        {
            var src = new Source();
            int vc = mesh.vertexCount;
            for (int i = 0; i < vc; i++) src.Verts.Add(new float[Size]);
            for (int i = 0; i < vc; i++) { var v = src.Verts[i]; v[C] = v[C + 1] = v[C + 2] = v[C + 3] = 1f; v[T + 3] = 1f; }

            var attrs = mesh.GetVertexAttributes();
            int streams = 0;
            foreach (var a in attrs) streams = Math.Max(streams, a.stream + 1);
            for (int s = 0; s < streams; s++)
            {
                int stride = mesh.GetVertexBufferStride(s);
                var data = ReadBuffer(mesh.GetVertexBuffer(s));
                int offset = 0;
                foreach (var a in attrs)
                {
                    if (a.stream != s) continue;
                    int dst = a.attribute switch
                    {
                        VertexAttribute.Position => P,
                        VertexAttribute.Normal => N,
                        VertexAttribute.Tangent => T,
                        VertexAttribute.TexCoord0 => U0,
                        VertexAttribute.TexCoord1 => U1,
                        VertexAttribute.Color => C,
                        _ => -1,
                    };
                    int cs = FormatSize(a.format);
                    if (dst >= 0)
                    {
                        int dims = Math.Min(a.dimension, dst == T || dst == C ? 4 : dst >= U0 && dst < C ? 2 : 3);
                        for (int i = 0; i < vc; i++)
                            for (int k = 0; k < dims; k++)
                                src.Verts[i][dst + k] = ReadComponent(data, i * stride + offset + k * cs, a.format);
                        if (dst == N) src.HasN = true;
                        if (dst == T) src.HasT = true;
                        if (dst == U0) src.HasU0 = true;
                        if (dst == U1) src.HasU1 = true;
                        if (dst == C) src.HasC = true;
                    }
                    offset += cs * a.dimension;
                }
            }
            foreach (var v in src.Verts)
            {
                var w = toWorld.MultiplyPoint3x4(new Vector3(v[P], v[P + 1], v[P + 2]));
                v[W] = w.x; v[W + 1] = w.y; v[W + 2] = w.z;
            }

            var ib = ReadBuffer(mesh.GetIndexBuffer());
            bool wide = mesh.indexFormat == IndexFormat.UInt32;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var sd = mesh.GetSubMesh(s);
                var list = new List<int>();
                if (sd.topology == MeshTopology.Triangles)
                    for (int i = 0; i < sd.indexCount; i++)
                    {
                        int at = sd.indexStart + i;
                        int idx = wide ? (int)BitConverter.ToUInt32(ib, at * 4) : BitConverter.ToUInt16(ib, at * 2);
                        list.Add(idx + sd.baseVertex);
                    }
                src.Subs.Add(list);
            }
            return src;
        }

        static float[] Lerp(float[] a, float[] b, float t)
        {
            var r = new float[Size];
            for (int i = 0; i < Size; i++) r[i] = a[i] + (b[i] - a[i]) * t;
            return r;
        }

        /// <summary>Keeps the part of the polygon where world[axis] is below (keepBelow) or above the threshold.</summary>
        static List<float[]> Clip(List<float[]> poly, int axis, float t, bool keepBelow)
        {
            var outp = new List<float[]>();
            if (poly.Count == 0) return outp;
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                float da = keepBelow ? t - a[W + axis] : a[W + axis] - t;
                float db = keepBelow ? t - b[W + axis] : b[W + axis] - t;
                bool ina = da >= 0, inb = db >= 0;
                if (ina) outp.Add(a);
                if (ina != inb) outp.Add(Lerp(a, b, da / (da - db)));
            }
            return outp;
        }

        /// <summary>Pieces of a triangle that lie outside the box (six slabs around it).</summary>
        static IEnumerable<List<float[]>> Outside(List<float[]> tri, Vector3 min, Vector3 max)
        {
            var inside = tri;
            for (int axis = 0; axis < 3; axis++)
            {
                yield return Clip(inside, axis, min[axis], true);
                yield return Clip(inside, axis, max[axis], false);
                inside = Clip(Clip(inside, axis, min[axis], false), axis, max[axis], true);
            }
        }

        /// <summary>
        /// Asks Unity to expose the mesh's GPU buffers for reading. Their new versions only exist after the next
        /// upload, so cut on a later frame.
        /// </summary>
        public static void PrepareRead(Mesh mesh)
        {
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        }

        /// <summary>Cuts the world-space box out of the renderer's mesh. Returns the number of triangles changed.</summary>
        public static int Cut(MeshFilter mf, Vector3 min, Vector3 max)
        {
            var mesh = mf.sharedMesh;
            var src = Read(mesh, mf.transform.localToWorldMatrix);
            var verts = new List<float[]>();
            var subs = new List<List<int>>();
            int changed = 0;
            foreach (var list in src.Subs)
            {
                var outIdx = new List<int>();
                for (int i = 0; i + 2 < list.Count; i += 3)
                {
                    var tri = new List<float[]> { src.Verts[list[i]], src.Verts[list[i + 1]], src.Verts[list[i + 2]] };
                    bool overlaps = true;
                    for (int axis = 0; axis < 3 && overlaps; axis++)
                    {
                        float lo = Mathf.Min(tri[0][W + axis], Mathf.Min(tri[1][W + axis], tri[2][W + axis]));
                        float hi = Mathf.Max(tri[0][W + axis], Mathf.Max(tri[1][W + axis], tri[2][W + axis]));
                        if (hi <= min[axis] || lo >= max[axis]) overlaps = false;
                    }
                    var pieces = overlaps ? Outside(tri, min, max) : new[] { tri };
                    if (overlaps) changed++;
                    foreach (var poly in pieces)
                    {
                        if (poly.Count < 3) continue;
                        int baseIdx = verts.Count;
                        verts.AddRange(poly);
                        for (int k = 1; k + 1 < poly.Count; k++)
                        {
                            outIdx.Add(baseIdx);
                            outIdx.Add(baseIdx + k);
                            outIdx.Add(baseIdx + k + 1);
                        }
                    }
                }
                subs.Add(outIdx);
            }
            if (changed == 0) return 0;

            int n = verts.Count;
            var pos = new Vector3[n];
            var nrm = new Vector3[n];
            var tan = new Vector4[n];
            var uv0 = new Vector2[n];
            var uv1 = new Vector2[n];
            var col = new Color[n];
            for (int i = 0; i < n; i++)
            {
                var v = verts[i];
                pos[i] = new Vector3(v[P], v[P + 1], v[P + 2]);
                nrm[i] = new Vector3(v[N], v[N + 1], v[N + 2]).normalized;
                tan[i] = new Vector4(v[T], v[T + 1], v[T + 2], v[T + 3] < 0 ? -1f : 1f);
                uv0[i] = new Vector2(v[U0], v[U0 + 1]);
                uv1[i] = new Vector2(v[U1], v[U1 + 1]);
                col[i] = new Color(v[C], v[C + 1], v[C + 2], v[C + 3]);
            }
            var m = new Mesh { name = mesh.name + "_CME_cut" };
            if (n > 65000) m.indexFormat = IndexFormat.UInt32;
            m.vertices = pos;
            if (src.HasN) m.normals = nrm;
            if (src.HasT) m.tangents = tan;
            if (src.HasU0) m.uv = uv0;
            if (src.HasU1) m.uv2 = uv1;
            if (src.HasC) m.colors = col;
            m.subMeshCount = subs.Count;
            for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s].ToArray(), s);
            m.RecalculateBounds();
            if (!src.HasN) m.RecalculateNormals();
            m.hideFlags = HideFlags.DontUnloadUnusedAsset;
            mf.sharedMesh = m;
            return changed;
        }
    }
}
