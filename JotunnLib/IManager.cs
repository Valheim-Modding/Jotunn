namespace Jotunn
{
    /// <summary>
    ///     The base class for all the library's various Managers
    /// </summary>
    internal interface IManager 
    {
        /// <summary>
        ///     Initialize manager class after all manager scripts have been added to the root game object
        /// </summary>
        void Init();
    }
}
