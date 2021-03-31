// JotunnLib
// a Valheim mod
// 
// File:    PatchInitializer.cs
// Project: JotunnLib

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using MonoMod.Utils;

namespace JotunnLib.Utils
{
    public abstract class PatchInitializer
    {
        public abstract void Init();

        public static void InitializePatches()
        {
            // Reflect through everything

            List<Tuple<Type,int>> types = new List<Tuple<Type,int>>();

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in asm.GetTypes().Where(x => x.BaseType == typeof(PatchInitializer)))
                {
                    var attributes = type.GetCustomAttributes(typeof(PatchPriorityAttribute), false).Cast<PatchPriorityAttribute>().FirstOrDefault();

                    if (attributes!=null)
                    {
                        types.Add(new Tuple<Type, int>(type,attributes.Priority));
                    }
                    else
                    {
                        types.Add(new Tuple<Type, int>(type, 0));
                    }
                }
            }

            foreach (Tuple<Type, int> entry in types.OrderBy(x => x.Item2))
            {
                PatchInitializer patch = Activator.CreateInstance(entry.Item1) as PatchInitializer;
                patch.Init();
            }
        }
    }
}