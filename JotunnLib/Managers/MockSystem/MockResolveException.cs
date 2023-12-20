using System;

namespace Jotunn.Managers.MockSystem
{
    /// <summary>
    ///     Exception that is thrown for a mock prefab, that could not be resolved to a real prefab.
    /// </summary>
    public class MockResolveException : Exception
    {
        /// <summary>
        ///     Name of the prefab that could not be resolved. Mock prefix is already removed.
        /// </summary>
        public string FailedMockName { get; private set; }

        /// <summary>
        ///     Path within the prefab that could not be resolved.
        /// </summary>
        public string FailedMockPathName { get; private set; }

        /// <summary>
        ///     Type of the prefab that could not be resolved.
        /// </summary>
        public Type MockType { get; private set; }

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="failedMockName"></param>
        [Obsolete("Use MockResolveException(string, string, Type) instead.")]
        public MockResolveException(string message, string failedMockName) : base(message)
        {
            FailedMockName = failedMockName;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="failedMockName"></param>
        /// <param name="mockType"></param>
        public MockResolveException(string message, string failedMockName, Type mockType) : base(message)
        {
            FailedMockName = failedMockName;
            MockType = mockType;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="failedMockName"></param>
        /// <param name="failedMockPathName"></param>
        /// <param name="mockType"></param>
        public MockResolveException(string message, string failedMockName, string failedMockPathName, Type mockType) : base(message)
        {
            FailedMockName = failedMockName;
            FailedMockPathName = failedMockPathName;
            MockType = mockType;
        }
    }
}
