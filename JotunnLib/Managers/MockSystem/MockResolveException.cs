using System;
using System.Collections.Generic;

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
        public string FailedMockPath { get; private set; }

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
        public MockResolveException(string message, string failedMockName, Type mockType) : base(ConstructMessage(message, failedMockName, string.Empty, mockType))
        {
            FailedMockName = failedMockName;
            MockType = mockType;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveException" /> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="failedMockName"></param>
        /// <param name="failedMockPath"></param>
        /// <param name="mockType"></param>
        public MockResolveException(string message, string failedMockName, IEnumerable<string> failedMockPath, Type mockType) : base(ConstructMessage(message, failedMockName, string.Join<string>("->", failedMockPath), mockType))
        {
            FailedMockName = failedMockName;
            FailedMockPath = string.Join<string>("->", failedMockPath);
            MockType = mockType;
        }

        private static string ConstructMessage(string message, string failedMockName, string failedMockPath, Type mockType)
        {
            if (string.IsNullOrEmpty(failedMockPath))
            {
                return $"Mock {mockType.Name} '{failedMockName}' could not be resolved. {message}";
            }

            return $"Mock {mockType.Name} at '{failedMockName}' with child path '{failedMockPath}' could not be resolved. {message}";
        }
    }
}
