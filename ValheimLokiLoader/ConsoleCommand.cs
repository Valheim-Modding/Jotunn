using System.Collections.Generic;
using UnityEngine;

namespace ValheimLokiLoader
{
    public abstract class ConsoleCommand
    {
        public abstract string Name { get; }
        public abstract string Help { get; }
        public abstract void Run(string[] args);
    }
}
