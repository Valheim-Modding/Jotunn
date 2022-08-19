using System;
using UnityEngine;

namespace Jotunn
{
    /// <summary>
    ///     Extends GameObject with a check if the GameObject is valid
    /// </summary>
    public static class GameObjectExtension
    {
        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this GameObject self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"GameObject must have a name !");
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

    /// <summary>
    ///     Extends ItemDrop with a TokenName
    /// </summary>
    public static class ItemDropExtension
    {
        /// <summary>
        ///     m_itemData.m_shared.m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this ItemDrop self) => self.m_itemData.m_shared.m_name;
    }

    /// <summary>
    ///     Extends StatusEffect with a TokenName and a check if the StatusEffect is valid so it can be added to the game.
    /// </summary>
    public static class RecipeExtension
    {
        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this Recipe self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"Recipe must have a name !");
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

    /// <summary>
    ///     Extends Piece with a TokenName
    /// </summary>
    public static class PieceExtension
    {
        /// <summary>
        ///     m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this Piece self) => self.m_name;
    }

    /// <summary>
    ///     Extends StatusEffect with a TokenName and a check if the StatusEffect is valid so it can be added to the game.
    /// </summary>
    public static class StatusEffectExtension
    {
        /// <summary>
        ///     m_name
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static string TokenName(this StatusEffect self) => self.m_name;

        /// <summary>
        ///     Check for validity
        /// </summary>
        /// <param name="self"></param>
        /// <returns></returns>
        public static bool IsValid(this StatusEffect self)
        {
            try
            {
                var name = self.name;
                if (name.IndexOf('(') > 0)
                {
                    name = name.Substring(self.name.IndexOf('(')).Trim();
                }
                if (string.IsNullOrEmpty(name))
                {
                    throw new Exception($"StatusEffect must have a name !");
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
