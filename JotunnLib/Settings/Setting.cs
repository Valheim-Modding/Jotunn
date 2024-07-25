using System;
using BepInEx;

namespace Jotunn.Settings
{
    public abstract class Setting<T>
    {
        public BepInPlugin SourceMod { get; set; }

        private T value;

        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnChanged?.Invoke();
            }
        }

        public event Action OnChanged;

        public Setting(BepInPlugin sourceMod)
        {
            this.SourceMod = sourceMod;
        }

        public void UpdateBinding(bool enabled)
        {
            if (enabled)
            {
                Bind();
            }
            else
            {
                Unbind();
            }
        }

        public abstract void Bind();

        public abstract void Unbind();
    }
}
