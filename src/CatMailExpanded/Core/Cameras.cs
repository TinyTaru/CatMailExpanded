using UnityEngine;

namespace CatMailExpanded.Core
{
    /// <summary>The camera the player is looking through (the game's camera is not tagged MainCamera).</summary>
    internal static class Cameras
    {
        public static Camera View
        {
            get
            {
                var dbg = Dev.DevConsole.ActiveDebugCamera;
                if (dbg) return dbg;
                try
                {
                    var pc = Il2Cpp.PlayerManager.Instance?.LocalPlayerEntity?.PlayerCamera?.Camera;
                    if (pc) return pc;
                }
                catch { }
                return Camera.main;
            }
        }
    }
}
