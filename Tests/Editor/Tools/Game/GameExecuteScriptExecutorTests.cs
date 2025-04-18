using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game; // GameExecuteScriptExecutor
using Sakurarin.UnityMcpServer.Runtime.Testing.Mocks; // MockUnityInstanceConnector
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // IUnityInstanceConnector
// using Newtonsoft.Json.Linq; // Not strictly needed for these tests

namespace Sakurarin.UnityMcpServer.Runtime.Tests.Editor.Tools.Game
{
    [TestFixture]
    public class GameExecuteScriptExecutorTests
    {
        private MockUnityInstanceConnector _mockConnector;
        private GameExecuteScriptExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector();
            _executor = new GameExecuteScriptExecutor(_mockConnector);
        }

        private Dictionary<string, object> CreateValidParameters(string instanceId = "test-inst-script", string script = "return 1;")
        {
            return new Dictionary<string, object>
            {
                { "instance_id", instanceId },
                { "script", script }
            };
        }

        [Test]
        public async Task ExecuteAsync_WithValidParameters_ReturnsSuccessStructure()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = true;
            var parameters = CreateValidParameters();

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Execution simulation should succeed, but got error: {result.ErrorMessage}");
            Assert.IsNotNull(result.ResultData, "ResultData should not be null on success simulation.");
            Assert.IsTrue(result.ResultData.ContainsKey("result"), "ResultData should contain 'result' key.");
            Assert.IsTrue(result.ResultData.ContainsKey("error"), "ResultData should contain 'error' key.");
            // In simulation, both result and error are expected to be null
            Assert.IsNull(result.ResultData["result"], "Simulated result should be null.");
            Assert.IsNull(result.ResultData["error"], "Simulated error should be null.");
        }

        [Test]
        public async Task ExecuteAsync_MissingInstanceId_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Remove("instance_id");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("instance_id"));
        }

        [Test]
        public async Task ExecuteAsync_MissingScript_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Remove("script");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("script"));
        }

        [Test]
        public async Task ExecuteAsync_EmptyScript_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters(script: "");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("script (string, non-empty)"));
        }

         [Test]
        public async Task ExecuteAsync_NullScript_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters["script"] = null; // Explicitly set script to null

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("script (string, non-empty)"));
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = false; // Simulate failure
            var parameters = CreateValidParameters();

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("Failed to get communicator"));
        }

        [Test]
        public async Task ExecuteAsync_NullParameters_ReturnsError()
        {
            // Act
            ToolResult result = await _executor.ExecuteAsync(null);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("Parameters dictionary is null"));
        }

        // Note: Testing the actual script execution result/error requires instance communication
        // and is beyond the scope of this unit test using a mock connector.
    }
} 