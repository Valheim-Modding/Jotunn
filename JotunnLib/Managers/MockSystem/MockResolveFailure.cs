using System;
using System.Collections.Generic;

namespace Jotunn.Managers.MockSystem
{
    /// <summary>
    ///     Failure that is tracked for a mock prefab which could not be resolved to a real prefab or other type.
    /// </summary>
    public class MockResolveFailure
    {
        /// <summary>
        ///     Accumulated list of the tracked MockResolveFailure.
        /// </summary>
        public static List<MockResolveFailure> MockResolveFailures { get; } = new List<MockResolveFailure>();

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveFailure" /> class.
        /// </summary>
        public MockResolveFailure(string message, string failedMockName, string failedMockPath, Type mockType)
        {
            Message = message;
            FailedMockName = failedMockName;
            FailedMockPath = failedMockPath;
            MockType = mockType;
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="MockResolveFailure" /> class.
        /// </summary>
        public MockResolveFailure(string message, string failedMockName, IEnumerable<string> failedMockPath, Type mockType)
        {
            Message = message;
            FailedMockName = failedMockName;
            FailedMockPath = string.Join<string>("->", failedMockPath);
            MockType = mockType;
        }

        /// <summary>
        ///     Additional message to display when printing warnings.
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        ///     Name of the type that could not be resolved. Mock prefix is already removed.
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

        private string ConstructMessage()
        {
            if (string.IsNullOrEmpty(FailedMockPath))
            {
                return $"Mock {MockType.Name} '{FailedMockName}' could not be resolved. {Message}";
            }

            return $"Mock {MockType.Name} at '{FailedMockName}' with child path '{FailedMockPath}' could not be resolved. {Message}";
        }

        /// <summary>
        ///     Prints warning messages for all the tracked MockResolveFailure.
        /// </summary>
        public static void PrintMockResolveFailures()
        {
            if (MockResolveFailures.Count == 0)
            {
                return;
            }

            int maximumPrinted = Math.Min(5, MockResolveFailures.Count);
            Logger.LogWarning($"{MockResolveFailures.Count} mocks could not be resolved. Details may be truncated.");
            foreach (var failure in MockResolveFailures.GetRange(0, maximumPrinted))
            {
                Logger.LogWarning(failure.ConstructMessage());
            }
        }
    }
}
