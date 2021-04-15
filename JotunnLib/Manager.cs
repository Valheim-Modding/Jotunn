using UnityEngine;

namespace JotunnLib
{
    /// <summary>
    ///     The base class for all the library's various Managers
    /// </summary>
    public interface IManager 
    {
        /// <summary>
        ///     Initialize manager class after all manager scripts have been added to the root game object
        /// </summary>
        internal abstract void Init();
    }
}
