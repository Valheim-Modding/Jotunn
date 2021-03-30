// JotunnLib
// a Valheim mod
// 
// File:    PatchInitializer.cs
// Project: JotunnLib

using System;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace JotunnLib.Utils
{
    public abstract class PatchInitializer
    {
        internal abstract void Init();

        public static void InitializePatches()
        {
            // Reflect through everything

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes().Where(x=>x.BaseType==typeof(PatchInitializer)))
                {
                    PatchInitializer initializer = (PatchInitializer)Activator.CreateInstance(type);
                    initializer.Init();
                }
            }
        }
    }
}