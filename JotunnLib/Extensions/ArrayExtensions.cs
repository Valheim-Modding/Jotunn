namespace Jotunn
{
    /// <summary>
    ///     Helper for Arrays
    /// </summary>
    public static class ArrayExtensions
    {
        /// <summary>
        ///     Populate an array with a default value
        /// </summary>
        /// <typeparam name="T">Array value type</typeparam>
        /// <param name="arr">Array instance</param>
        /// <param name="value">Default value</param>
        /// <returns>Reference to the array instance</returns>
        public static T[] Populate<T>(this T[] arr, T value)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = value;
            }

            return arr;
        }
    }
}
