using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game; // GamePlaceObjectExecutor
using Sakurarin.UnityMcpServer.Runtime.Testing.Mocks; // Added correct using for Mocks
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // IUnityInstanceConnector
using Newtonsoft.Json.Linq; // For JObject
using UnityEngine; // For Vector3, Quaternion

namespace Sakurarin.UnityMcpServer.Runtime.Tests.Editor.Tools.Game
{
    [TestFixture]
    public class GamePlaceObjectExecutorTests
    {
        private MockUnityInstanceConnector _mockConnector;
        private GamePlaceObjectExecutor _executor;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector();
            _executor = new GamePlaceObjectExecutor(_mockConnector);
        }

        private Dictionary<string, object> CreateValidParameters(string instanceId = "test-inst", string objectId = "Cube",
                                                                  float posX = 0f, float posY = 0f, float posZ = 0f,
                                                                  bool includeRotation = false,
                                                                  float rotX = 0f, float rotY = 0f, float rotZ = 0f, float rotW = 1f)
        {
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", instanceId },
                { "object_id", objectId },
                { "position", new JObject { ["x"] = posX, ["y"] = posY, ["z"] = posZ } }
            };
            if (includeRotation)
            {
                parameters.Add("rotation", new JObject { ["x"] = rotX, ["y"] = rotY, ["z"] = rotZ, ["w"] = rotW });
            }
            return parameters;
        }

        [Test]
        public async Task ExecuteAsync_WithValidParameters_NoRotation_ReturnsSuccess()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = true;
            var parameters = CreateValidParameters(posY: 1.0f);

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Execution should succeed, but got error: {result.ErrorMessage}");
            Assert.IsNotNull(result.ResultData);
            Assert.IsTrue(result.ResultData.TryGetValue("success", out var successVal) && (bool)successVal);
            Assert.IsTrue(result.ResultData.ContainsKey("placed_object_instance_id")); // Value can be null
        }

         [Test]
        public async Task ExecuteAsync_WithValidParameters_WithRotation_ReturnsSuccess()
        {
            // Arrange
            _mockConnector.SimulateConnectionSuccess = true;
            var parameters = CreateValidParameters(includeRotation: true, rotY: 0.707f, rotW: 0.707f);

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Execution should succeed, but got error: {result.ErrorMessage}");
            Assert.IsNotNull(result.ResultData);
             Assert.IsTrue(result.ResultData.TryGetValue("success", out var successVal) && (bool)successVal);
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
        public async Task ExecuteAsync_MissingObjectId_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Remove("object_id");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("object_id"));
        }

        [Test]
        public async Task ExecuteAsync_MissingPosition_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters.Remove("position");

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("position"));
        }

        [Test]
        public async Task ExecuteAsync_InvalidPositionFormat_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidParameters();
            parameters["position"] = new JObject { ["x"] = 1.0f, ["y"] = "invalid" }; // Missing 'z', invalid 'y'

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.That(result.ErrorMessage, Does.Contain("Invalid or missing 'y'"), "Error message should indicate the specific invalid component.");
        }

        [Test]
        public async Task ExecuteAsync_MalformedRotation_UsesDefaultAndSucceeds()
        {
            // Arrange
             _mockConnector.SimulateConnectionSuccess = true;
            var parameters = CreateValidParameters();
            parameters["rotation"] = new JObject { ["x"] = 0.5f }; // Malformed rotation

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            // Should still succeed as rotation is optional and defaults on error
            Assert.IsFalse(result.IsError, $"Execution should succeed even with malformed rotation, but got error: {result.ErrorMessage}");
            Assert.IsTrue(result.ResultData.TryGetValue("success", out var successVal) && (bool)successVal);
            // TODO: Potentially check log output for the warning about malformed rotation
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
    }
} 