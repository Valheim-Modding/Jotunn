using System;

namespace Jotunn.Utils
{
    /// <summary>
    ///     Priority attribute for PatchInitalizer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PatchInitAttribute : Attribute
    {
        /// <summary>
        ///     The patch priority.
        ///     <para>
        ///         negative - early
        ///         <br />
        ///         zero - neutral
        ///         <br />
        ///         positive - late
        ///     </para>
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        ///     Patch Init Attribute
        /// </summary>
        /// <param name="priority"><see cref="Priority"/></param>
        public PatchInitAttribute(int priority)
        {
            Priority = priority;
        }
    }
}
