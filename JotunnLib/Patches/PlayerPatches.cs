using JotunnLib.Events;
using JotunnLib.Managers;
using JotunnLib.Utils;

namespace JotunnLib.Patches
{
    internal class PlayerPatches
    {
        [PatchInit(0)]
        public static void Init()
        {
            On.Player.OnSpawned += Player_OnSpawned;
            On.Player.PlacePiece += Player_PlacePiece;
        }

        private static bool Player_PlacePiece(On.Player.orig_PlacePiece orig, Player self, Piece piece)
        {
            var result = orig(self, piece);

            EventManager.OnPlayerPlacedPiece(new PlayerPlacedPieceEventArgs
            {
                Player = self,
                Piece = piece,
                Position = self.m_placementGhost.transform.position,
                Rotation = self.m_placementGhost.transform.rotation,
                Success = result
            });

            return result;
        }

        private static void Player_OnSpawned(On.Player.orig_OnSpawned orig, Player self)
        {
#if DEBUG

            // Temp: disable valkyrie animation during testing for sanity reasons
            self.m_firstSpawn = false;
#endif

            EventManager.OnPlayerSpawned(self);

            orig(self);
        }
    }
}