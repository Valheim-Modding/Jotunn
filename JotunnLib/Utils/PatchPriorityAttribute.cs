// JotunnLib
// a Valheim mod
// 
// File:    PatchPriorityAttribute.cs
// Project: JotunnLib

using System;

namespace JotunnLib.Utils
{
    public class PatchPriorityAttribute: Attribute
    {
        public int Priority { get; set; }

        public PatchPriorityAttribute(int priority)
        {
            Priority = priority;
        }
    }
}