// JotunnLib
// a Valheim mod
// 
// File:    PatchInitAttribute.cs
// Project: JotunnLib

using System;

namespace JotunnLib.Utils
{
    /// <summary>
    ///     Priority attribute for PatchInitalizer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchInitAttribute : Attribute
    {
        /// <summary>
        ///     The patch priority.
        ///     <para>
        ///         negative - early
        ///         <br />
        ///         zero - neutral
        ///         <br />
        ///         positive - late
        ///     </para>
        /// </summary>
        public int Priority { get; set; }

        public PatchInitAttribute(int priority)
        {
            Priority = priority;
        }
    }
}