using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.Tools;
using Sakurarin.UnityMcpServer.Runtime.Tools.Log; // Namespace for LogMessageExecutor
using UnityEngine;
using UnityEngine.TestTools;

namespace Sakurarin.UnityMcpServer.Runtime.Tests.Editor
{
    public class LogMessageExecutorTests
    {
        private LogMessageExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _executor = new LogMessageExecutor();
            // Ensure default log level allows Info messages for verification
            LoggingService.SetLogLevel(LogLevel.Info);
        }

        [Test]
        public async Task ExecuteAsync_WithValidParameters_ReturnsSuccessAndLogsMessage()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-instance-1" },
                { "level", "Info" },
                { "message", "This is a test log message." }
            };
            string expectedLogMessage = "[UnityMcpServer] [INFO] [Instance test-instance-1] This is a test log message.";

            // Expect a log message matching the format from LoggingService
            LogAssert.Expect(LogType.Log, expectedLogMessage);

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsNotNull(result, "Result should not be null.");
            Assert.IsFalse(result.IsError, "Result should indicate success.");
            Assert.IsNotNull(result.Content, "Success result should have Content.");
            Assert.IsTrue(result.Content.ContainsKey("success"), "Success Content should contain 'success' key.");
            Assert.AreEqual(true, result.Content["success"], "Success Content['success'] should be true.");

            // LogAssert.Expect automatically checks if the log was received during the test execution.
            // If the log wasn't received, the test will fail here or at TearDown.
        }

        [Test]
        public async Task ExecuteAsync_WithValidParametersDebug_ReturnsSuccessAndLogsMessage()
        {
             // Arrange
             LoggingService.SetLogLevel(LogLevel.Debug); // Set level to Debug for this test
             var parameters = new Dictionary<string, object>
             {
                 { "instance_id", "debug-instance" },
                 { "level", "debug" }, // Lowercase level
                 { "message", "A debug log." }
             };
             string expectedLogMessage = "[UnityMcpServer] [DEBUG] [Instance debug-instance] A debug log.";
             LogAssert.Expect(LogType.Log, expectedLogMessage);

             // Act
             ToolResult result = await _executor.ExecuteAsync(parameters);

             // Assert
             Assert.IsFalse(result.IsError);
        }

        [Test]
        public async Task ExecuteAsync_WithValidParametersWarn_ReturnsSuccessAndLogsWarning()
        {
             // Arrange
             var parameters = new Dictionary<string, object>
             {
                 { "instance_id", "warn-instance" },
                 { "level", "WARN" }, // Uppercase level
                 { "message", "A warning log." }
             };
             string expectedLogMessage = "[UnityMcpServer] [WARN] [Instance warn-instance] A warning log.";
             LogAssert.Expect(LogType.Warning, expectedLogMessage); // Expect LogType.Warning

             // Act
             ToolResult result = await _executor.ExecuteAsync(parameters);

             // Assert
             Assert.IsFalse(result.IsError);
        }

         [Test]
        public async Task ExecuteAsync_WithValidParametersError_ReturnsSuccessAndLogsError()
        {
             // Arrange
             var parameters = new Dictionary<string, object>
             {
                 { "instance_id", "error-instance" },
                 { "level", "Error" },
                 { "message", "An error log." }
             };
             string expectedLogMessage = "[UnityMcpServer] [ERROR] [Instance error-instance] An error log.";
             LogAssert.Expect(LogType.Error, expectedLogMessage); // Expect LogType.Error

             // Act
             ToolResult result = await _executor.ExecuteAsync(parameters);

             // Assert
             Assert.IsFalse(result.IsError);
        }

        [TestCase(null)] // Missing instance_id
        [TestCase("")]   // Empty instance_id
        public async Task ExecuteAsync_MissingOrEmptyInstanceId_ReturnsError(string instanceId)
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", instanceId },
                { "level", "Info" },
                { "message", "Test" }
            };
             if (instanceId == null) parameters.Remove("instance_id");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
        }

        [TestCase(null)] // Missing level
        [TestCase("")]   // Empty level
        public async Task ExecuteAsync_MissingOrEmptyLevel_ReturnsError(string level)
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test" },
                { "level", level },
                { "message", "Test" }
            };
             if (level == null) parameters.Remove("level");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
             Assert.AreEqual("Missing or invalid required parameter: level (string).", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_MissingMessage_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test" },
                { "level", "Info" }
                // message is missing
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing required parameter: message (string).", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_NullMessage_ReturnsSuccess()
        {
            // Arrange
            // Design decision: Assume null message is treated as empty string and allowed
             var parameters = new Dictionary<string, object>
            {
                { "instance_id", "null-msg" },
                { "level", "Info" },
                { "message", null }
            };
             string expectedLogMessage = "[UnityMcpServer] [INFO] [Instance null-msg] "; // Expect empty message logged
             LogAssert.Expect(LogType.Log, expectedLogMessage);


            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, "Result should be success even with null message.");
            // LogAssert check is performed implicitly
        }

        [TestCase("InvalidLevel")]
        [TestCase("Information")]
        [TestCase("123")]
        public async Task ExecuteAsync_InvalidLevelString_ReturnsError(string invalidLevel)
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test" },
                { "level", invalidLevel },
                { "message", "Test" }
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            StringAssert.StartsWith($"Invalid log level specified: '{invalidLevel}'.", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_NullParameters_ReturnsError()
        {
            // Act
            ToolResult result = await _executor.ExecuteAsync(null);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Parameters dictionary is null.", result.ErrorMessage);
        }
    }
} 