using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game; // GameSendKeyEventExecutor
using Sakurarin.UnityMcpServer.Runtime.Testing.Mocks; // Added correct using for Mocks
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // IUnityInstanceConnector

namespace Sakurarin.UnityMcpServer.Runtime.Tests.Editor.Tools.Game
{
    [TestFixture]
    public class GameSendKeyEventExecutorTests
    {
        private MockUnityInstanceConnector _mockConnector;
        private GameSendKeyEventExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            // Create mock and executor instances for each test
            _mockConnector = new MockUnityInstanceConnector();
            _executor = new GameSendKeyEventExecutor(_mockConnector);
        }

        [Test]
        public async Task ExecuteAsync_WithValidParameters_ReturnsSuccess()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = true;
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-1" },
                { "key_code", "Space" }
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Execution should succeed, but got error: {result.ErrorMessage}");
            // SimpleSuccess might have null ResultData or an empty one, depending on ToolResult implementation
            // Assert.IsNotNull(result.ResultData, "ResultData should not be null on success.");
        }

        [Test]
        public async Task ExecuteAsync_MissingInstanceId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                // Missing instance_id
                { "key_code", "Enter" }
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError, "Execution should fail due to missing instance_id.");
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("instance_id"));
        }

        [Test]
        public async Task ExecuteAsync_MissingKeyCode_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-2" }
                // Missing key_code
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError, "Execution should fail due to missing key_code.");
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("key_code"));
        }

        [Test]
        public async Task ExecuteAsync_NullInstanceId_ReturnsError()
        {
             // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", null },
                { "key_code", "Escape" }
            };
            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.IsTrue(result.ErrorMessage.Contains("instance_id"));
        }

        [Test]
        public async Task ExecuteAsync_EmptyKeyCode_ReturnsError()
        {
             // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-3" },
                { "key_code", "" } // Empty string
            };
            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
             Assert.IsTrue(result.ErrorMessage.Contains("key_code"));
        }


        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = false; // Simulate connection failure
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-fail" },
                { "key_code", "A" }
            };

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError, "Execution should fail due to connection failure simulation.");
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Failed to get communicator"));
        }

        [Test]
        public async Task ExecuteAsync_NullParameters_ReturnsError()
        {
            // Arrange
            Dictionary<string, object> parameters = null;

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("Parameters dictionary is null"));
        }

    }
} 