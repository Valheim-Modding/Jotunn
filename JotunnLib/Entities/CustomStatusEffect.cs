namespace JotunnLib.Entities
{
    public class CustomStatusEffect
    {
        public StatusEffect StatusEffect;
        public bool FixReference;

        public CustomStatusEffect(StatusEffect statusEffect, bool fixReference)
        {
            StatusEffect = statusEffect;
            FixReference = fixReference;
        }
    }
}