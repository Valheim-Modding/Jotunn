using JotunnLib.Managers;
using System;

namespace JotunnLib
{
    public static class ItemDropExtension
    {
        public static string TokenName(this ItemDrop self) => self.m_itemData.m_shared.m_name;

        public static bool IsValid(this ItemDrop self)
        {
            try
            {
                var tokenName = self.TokenName();
                if (tokenName[0] != LocalizationManager.TokenFirstChar)
                {
                    throw new Exception($"Item name first char should be $ for token lookup ! (current item name : {tokenName})");
                }

                var hasIcon = self.m_itemData.m_shared.m_icons.Length > 0;
                if (!hasIcon)
                {
                    throw new Exception($"ItemDrop should have atleast one icon !");
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e);

                return false;
            }
        }
    }
}
