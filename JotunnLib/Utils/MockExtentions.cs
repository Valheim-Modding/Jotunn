using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JotunnLib.Utils
{
    internal static class ItemDropMockFix
    {
        private static bool _enabled;

        internal static void Switch(bool enable)
        {
            if (enable)
            {
                if (!_enabled)
                {
                    On.ItemDrop.Awake += SilenceErrors;
                    _enabled = enable;
                }
            }
            else
            {
                On.ItemDrop.Awake -= SilenceErrors;
                _enabled = enable;
            }
        }

        private static void SilenceErrors(On.ItemDrop.orig_Awake orig, ItemDrop self)
        {
            try
            {
                orig(self);
            }
            catch (Exception)
            {

            }
        }

        internal static bool IsValid(this ObjectDB self)
        {
            return self.m_items.Count > 0;
        }
    }
}
