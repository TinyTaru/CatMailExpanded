using UnityEngine;

namespace CatMailExpanded.Content
{
    /// <summary>Axis-aligned box (managed; UnityEngine.Bounds.Contains is broken under this interop).</summary>
    internal readonly struct Box
    {
        public readonly Vector3 Min, Max;
        public Box(Vector3 min, Vector3 max) { Min = min; Max = max; }
        public bool Contains(Vector3 p) =>
            p.x >= Min.x && p.x <= Max.x && p.y >= Min.y && p.y <= Max.y && p.z >= Min.z && p.z <= Max.z;
        public Vector3 Center => (Min + Max) * 0.5f;
        public Vector3 Size => Max - Min;
    }

    /// <summary>World-space areas used by the new mechanics.</summary>
    internal static class Zones
    {
        // Outdoor yard deck west of the storage corridor's service door (see Yard.cs).
        public static readonly Box Yard = new(new Vector3(-13.8f, -1.5f, -11.8f), new Vector3(-2.35f, 6f, 2.55f));

        // Break room interior (measured from p_BreakRoom renderer bounds).
        public static readonly Box BreakRoom = new(new Vector3(7.65f, -0.5f, 17.6f), new Vector3(12.8f, 4.6f, 26.3f));

        // Planetarium interior, up to the corridor wall (see Planetarium.cs).
        public static readonly Box Planetarium = new(new Vector3(-1.6f, -0.5f, 21.2f), new Vector3(4.45f, 6.6f, 26.9f));

        public static bool InYard(Vector3 p) => Yard.Contains(p);
        public static bool InPlanetarium(Vector3 p) => Planetarium.Contains(p);
        public static bool InBreakRoom(Vector3 p) => BreakRoom.Contains(p);
    }
}
