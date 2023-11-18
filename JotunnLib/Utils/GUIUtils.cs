using UnityEngine;
using UnityEngine.Rendering;

namespace Jotunn.Utils
{
    internal static class GUIUtils
    {
        public static bool IsHeadless { get; } = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    }
}
