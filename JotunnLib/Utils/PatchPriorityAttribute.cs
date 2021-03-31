// JotunnLib
// a Valheim mod
// 
// File:    PatchPriorityAttribute.cs
// Project: JotunnLib

using System;

namespace JotunnLib.Utils
{
    /// <summary>
    /// Attribute to set Patch priority
    /// negative - early
    /// zero - neutral
    /// positive - late
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PatchPriorityAttribute : Attribute
    {
        public int Priority { get; set; }

        public PatchPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}