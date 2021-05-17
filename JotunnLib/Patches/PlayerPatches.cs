using Jotunn.Utils;

namespace Jotunn.Patches
{
    internal class PlayerPatches
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.Player.OnSpawned += DisableValkyrieOnDebug;
        }

        private static void DisableValkyrieOnDebug(On.Player.orig_OnSpawned orig, Player self)
        {
#if DEBUG
            // Disable valkyrie animation during testing for sanity reasons
            self.m_firstSpawn = false;
#endif
            orig(self);
        }
    }
}
