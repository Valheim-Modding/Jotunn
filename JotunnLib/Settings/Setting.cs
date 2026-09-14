using System;
using BepInEx;

namespace Jotunn.Settings
{
    /// <summary>
    ///     Base class for in-game settings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Setting<T>
    {
        /// <summary>
        ///     The mod, where the object this setting applies to
        /// </summary>
        public BepInPlugin SourceMod { get; set; }

        private T value;

        /// <summary>
        ///     The value of this setting.
        ///     When set, the <see cref="OnChanged"/> event is invoked.<br />
        ///     <br />
        ///     The value depends on the underlying configuration system and might be not initialized immediately.
        /// </summary>
        public T Value
        {
            get => value;
            set
            {
                this.value = value;
                OnChanged?.Invoke();
            }
        }

        /// <summary>
        ///     Event that is invoked when the value of this setting changes
        /// </summary>
        public event Action OnChanged;

        /// <summary>
        ///     Create a new setting object
        /// </summary>
        /// <param name="sourceMod"></param>
        public Setting(BepInPlugin sourceMod)
        {
            this.SourceMod = sourceMod;
        }

        /// <summary>
        ///     Update the binding of this setting and applies it to the underlying configuration system
        /// </summary>
        /// <param name="enabled">whether the setting should be bound</param>
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

        /// <summary>
        ///     Bind this setting to the underlying configuration system.
        ///     E.g. maps and saves the value to a config file
        /// </summary>
        public abstract void Bind();

        /// <summary>
        ///     Unbind this setting from the underlying configuration system.
        ///     E.g. removes the value from a config file
        /// </summary>
        public abstract void Unbind();
    }
}
