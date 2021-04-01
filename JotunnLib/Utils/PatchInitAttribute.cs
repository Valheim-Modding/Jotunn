// JotunnLib
// a Valheim mod
// 
// File:    PatchInitAttribute.cs
// Project: JotunnLib

using System;

namespace JotunnLib.Utils
{
    /// <summary>
    /// Priority attribute for PatchInitalizer
    /// negative - early
    /// zero - neutral
    /// positive - late
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchInitAttribute: Attribute
    {
        public int Priority { get; set; }

        public PatchInitAttribute(int priority)
        {
            Priority = priority;
        }
    }
}