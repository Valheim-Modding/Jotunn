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
        ///     Creates a new instance of the <see cref="MockResolveException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="failedMockName"></param>
        public MockResolveException(string message, string failedMockName) : base(message)
        {
            FailedMockName = failedMockName;
        }
    }
}
