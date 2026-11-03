using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>Tiny delayed-action queue driven from the mod's OnUpdate (avoids Il2Cpp coroutine plumbing).</summary>
    internal static class Scheduler
    {
        static readonly List<(float time, Action action)> _queue = new();

        public static void After(float seconds, Action action) => _queue.Add((Time.time + seconds, action));

        public static void Update()
        {
            if (_queue.Count == 0) return;
            float now = Time.time;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                if (_queue[i].time > now) continue;
                var a = _queue[i].action;
                _queue.RemoveAt(i);
                try { a(); } catch (Exception e) { Plugin.Log.Warning("Scheduled action failed: " + e.Message); }
            }
        }

        public static void Clear() => _queue.Clear();
    }

    internal static class Lighting
    {
        /// <summary>0 during the day, 1 at night (used to dim unlit effects).</summary>
        public static float NightFactor()
        {
            try
            {
                var gtm = Il2Cpp.GameTimeManager.Instance;
                if (gtm == null) return 0f;
                return gtm.CurrentTimePeriod switch
                {
                    Il2Cpp.GameTimePeriod.Night => 1f,
                    Il2Cpp.GameTimePeriod.Evening => 0.45f,
                    Il2Cpp.GameTimePeriod.Dawn => 0.45f,
                    Il2Cpp.GameTimePeriod.EveningRecap => 0.6f,
                    Il2Cpp.GameTimePeriod.DawnRecap => 0.6f,
                    _ => 0f,
                };
            }
            catch { return 0f; }
        }
    }
}
