using UnityEngine;
using UnityEngine.SceneManagement;

namespace Jotunn.DebugUtils
{
    internal class DebugHelper : MonoBehaviour
    {
        private void Awake()
        {
            On.ZNet.Awake += ZNet_Awake;
        }

        private void ZNet_Awake(On.ZNet.orig_Awake orig, ZNet self)
        {
            Logger.LogDebug($"ZNet awoken. IsServer: {self.IsServer()} IsDedicated: {self.IsDedicated()}");
            orig(self);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            { // Set a breakpoint here to break on F6 key press
            }
        }

        private void OnGUI()
        {
            // Display version in main menu
            if (SceneManager.GetActiveScene().name == "start")
            {
                UnityEngine.GUI.Label(new Rect(Screen.width - 100, 5, 100, 25), "Jötunn v" + Main.Version);
            }
        }

    }
}
