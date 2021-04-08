namespace JotunnLib.Entities
{
    public class CustomStatusEffect
    {
        /// <summary>
        ///     The <see cref="StatusEffect"/> for this custom status effect.
        /// </summary>
        public StatusEffect StatusEffect { get; set; }

        /// <summary>
        ///     Indicator if references from <see cref="Mock"/>s will be replaced at runtime.
        /// </summary>
        public bool FixReference { get; set; }

        /// <summary>
        ///     Custom status effect from a <see cref="global::StatusEffect"/>.<br />
        ///     Can fix references for <see cref="Mock"/>s.
        /// </summary>
        /// <param name="statusEffect">A preloaded <see cref="global::StatusEffect"/></param>
        /// <param name="fixReference">If true references for <see cref="Mock"/> objects get resolved at runtime by Jötunn.</param>
        public CustomStatusEffect(StatusEffect statusEffect, bool fixReference)
        {
            StatusEffect = statusEffect;
            FixReference = fixReference;
        }

        public override bool Equals(object obj)
        {
            return obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            return StatusEffect.name.GetStableHashCode();
        }

        public override string ToString()
        {
            return StatusEffect.name;
        }
    }
}