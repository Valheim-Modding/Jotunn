using JotunnLib.Configs;
using JotunnLib.Managers;
using UnityEngine;

namespace JotunnLib.Entities
{
    /// <summary>
    ///     Main interface for adding custom <see cref="KeyHints"/> to the game.<br />
    ///     All custom key hints have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomKeyHint
    {
        /// <summary>
        ///     Item for which the KeyHint should be displayed when equipped.<br />
        ///     Must be the name of the prefab as registered in the <see cref="ItemManager"/>.
        /// </summary>
        public string Item { get; set; }

        /// <summary>
        ///     The <see cref="GameObject"/> representing the KeyHint.
        /// </summary>
        public GameObject KeyHint { get; set; }

        /// <summary>
        ///     Custom key hint for an item with pre-built prefab.<br />
        /// </summary>
        /// <param name="item">Name of the item this key hint is bound to.</param>
        /// <param name="keyHint">Prefab of the key hint to display.</param>
        public CustomKeyHint(string item, GameObject keyHint)
        {
            Item = item;
            KeyHint = keyHint;
        }

        /// <summary>
        ///     Custom key hint for an item from a <see cref="KeyHintConfig"/>.<br />
        ///     The <see cref="GameObject"/> is created automatically by Jötunn at runtime.
        /// </summary>
        /// <param name="keyHintConfig">The <see cref="KeyHintConfig"/> for a custom key hint.</param>
        public CustomKeyHint(KeyHintConfig keyHintConfig)
        {
            Item = keyHintConfig.Item;
            KeyHint = keyHintConfig.GetKeyHint();
        }

        /// <summary>
        ///     Checks if a custom key hint is valid (i.e. has an item and a <see cref="GameObject"/>).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return !string.IsNullOrEmpty(Item) && KeyHint;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return Item.GetStableHashCode();
        }

        public override string ToString()
        {
            return Item;
        }
    }
}