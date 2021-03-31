// JotunnLib
// a Valheim mod
// 
// File:    PatchPriorityAttribute.cs
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
    public class PatchPriorityAttribute: Attribute
    {
        public int Priority { get; set; }

        public PatchPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}