using Jotunn.Managers;

namespace Jotunn.Entities
{
    /// <summary>
    ///     Main interface for adding custom status effects to the game.<br />
    ///     All custom status effects have to be wrapped inside this class to add it to Jötunns <see cref="ItemManager"/>.
    /// </summary>
    public class CustomStatusEffect
    {
        /// <summary>
        ///     The <see cref="global::StatusEffect"/> for this custom status effect.
        /// </summary>
        public StatusEffect StatusEffect { get; set; }

        /// <summary>
        ///     Indicator if references from <see cref="Entities.Mock{T}"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom status effect from a <see cref="global::StatusEffect"/>.<br />
        ///     Can fix references for <see cref="Entities.Mock{T}"/>s.
        /// </summary>
        /// <param name="statusEffect">A preloaded <see cref="global::StatusEffect"/></param>
        /// <param name="fixReference">If true references for <see cref="Entities.Mock{T}"/> objects get resolved at runtime by Jötunn.</param>
        public CustomStatusEffect(StatusEffect statusEffect, bool fixReference)
        {
            StatusEffect = statusEffect;
            FixReference = fixReference;
        }

        /// <summary>
        ///     Checks if a custom status effect is valid (i.e. has a <see cref="global::StatusEffect"/>).
        /// </summary>
        /// <returns>true if all criteria is met</returns>
        public bool IsValid()
        {
            return StatusEffect != null && StatusEffect.IsValid();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return StatusEffect.name.GetStableHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return StatusEffect.name;
        }
    }
}
