using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>Helpers for finding scene objects and building props/colliders at runtime.</summary>
    internal static class World
    {
        public static GameObject Find(string exactName, string pathContains = null)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (!go || go.name != exactName || !go.scene.IsValid()) continue;
                if (pathContains != null && !PathOf(go.transform).Contains(pathContains)) continue;
                return go;
            }
            return null;
        }

        public static List<GameObject> FindAll(string exactName)
        {
            var list = new List<GameObject>();
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go && go.name == exactName && go.scene.IsValid()) list.Add(go);
            return list;
        }

        public static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        public static GameObject Root(string name)
        {
            var go = GameObject.Find(name);
            if (go) UnityEngine.Object.Destroy(go);
            return new GameObject(name);
        }

        /// <summary>
        /// Copies only the visuals (meshes + materials) of a scene object into a new GameObject,
        /// so game props can be reused without duplicating their gameplay/network components.
        /// </summary>
        public static GameObject CopyVisual(GameObject source, Transform parent, Vector3 pos, float yaw, float scale = 1f, bool collider = true)
        {
            if (!source) return null;
            var root = new GameObject("CME_" + source.name);
            root.transform.SetParent(parent, false);
            var inv = source.transform.worldToLocalMatrix;
            var rot = Quaternion.Euler(0, yaw, 0);
            bool any = false;
            Vector3 bmin = Vector3.positiveInfinity, bmax = Vector3.negativeInfinity;
            foreach (var mr in source.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!mr.enabled || !mr.gameObject.activeInHierarchy) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (!mf || !mf.sharedMesh) continue;
                // skip outline / preview helpers
                var n = mr.gameObject.name;
                if (n.Contains("Outline") || n.Contains("Preview") || n.Contains("Collider")) continue;
                var child = new GameObject(n);
                child.transform.SetParent(root.transform, false);
                var local = inv * mr.transform.localToWorldMatrix;
                child.transform.localPosition = rot * (Vector3)local.GetColumn(3) * scale;
                child.transform.localRotation = rot * local.rotation;
                child.transform.localScale = local.lossyScale * scale;
                child.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = child.AddComponent<MeshRenderer>();
                r.sharedMaterials = mr.sharedMaterials;
                any = true;
            }
            root.transform.position = pos;
            if (!any) { UnityEngine.Object.Destroy(root); return null; }
            if (collider)
            {
                var b = RendererBounds(root);
                var bc = root.AddComponent<BoxCollider>();
                bc.center = root.transform.InverseTransformPoint(b.center);
                bc.size = b.size;
            }
            return root;
        }

        public static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }

        public static BoxCollider Box(Transform parent, string name, Vector3 center, Vector3 size, int layer)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.layer = layer;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
            return bc;
        }

        public static GameObject Mesh(string mesh, string texture, Transform parent, Vector3 pos, float yaw, bool collider = false)
        {
            var go = ModAssets.Prop(mesh, texture, parent, pos, Quaternion.Euler(0, yaw, 0), collider);
            return go;
        }

        public static Light PointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject("CME_Light");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        public static void SetLayerRecursive(GameObject go, int layer)
        {
            go.layer = layer;
            for (int i = 0; i < go.transform.childCount; i++) SetLayerRecursive(go.transform.GetChild(i).gameObject, layer);
        }
    }
}
