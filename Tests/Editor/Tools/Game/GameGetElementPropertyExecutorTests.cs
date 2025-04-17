using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;
using UnityEngine.TestTools;
using UnityEngine;

namespace Sakurarin.UnityMcpServer.Tests.Editor.Tools.Game
{
    // Consider moving mock to a shared location
    public class MockUnityInstanceConnector_ForGetProp : IUnityInstanceConnector
    {
        public bool ShouldConnectSuccessfully { get; set; } = true;
        public string InstanceIdToConnect { get; set; } = string.Empty;
        public TcpCommunicator CommunicatorToReturn { get; set; } = null;

        public Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000)
        {
            return ShouldConnectSuccessfully && instanceId == InstanceIdToConnect
                ? Task.FromResult(CommunicatorToReturn)
                : Task.FromResult<TcpCommunicator>(null);
        }
        public void DisconnectInstance(string instanceId) { }
        public Task<bool> SendMessageToInstanceAsync(string instanceId, string message) => Task.FromResult(true);
        public void Dispose() { }
    }

    public class GameGetElementPropertyExecutorTests
    {
        private GameGetElementPropertyExecutor _executor;
        private MockUnityInstanceConnector_ForGetProp _mockConnector;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector_ForGetProp();
            _executor = new GameGetElementPropertyExecutor(_mockConnector);
        }

        private Dictionary<string, object> CreateValidGetPropertyParams(string instanceId = "getprop-inst-1", string propName = "text") => new Dictionary<string, object>
        {
            { "instance_id", instanceId },
            { "element_id", "label_status" },
            { "property_name", propName }
        };

        [Test]
        public async Task ExecuteAsync_ValidParametersAndConnection_SuccessReturnsValue()
        {
            // Arrange
            string propName = "visibility";
            var parameters = CreateValidGetPropertyParams("getprop-inst-ok", propName);
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "getprop-inst-ok";
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999);
            string expectedValue = $"simulated_value_for_{propName}";

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Expected success but got error: {result.ErrorMessage}");
            Assert.IsNull(result.ErrorMessage);
            Assert.IsNotNull(result.ResultData, "ResultData should not be null on success");
            Assert.IsTrue(result.ResultData.ContainsKey("value"), "ResultData should contain 'value' key");
            Assert.AreEqual(expectedValue, result.ResultData["value"], "Returned value does not match expected simulated value");
            
             _mockConnector.CommunicatorToReturn?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidGetPropertyParams("getprop-inst-fail", "enabled");
            _mockConnector.ShouldConnectSuccessfully = false;
            _mockConnector.InstanceIdToConnect = "getprop-inst-fail";
            _mockConnector.CommunicatorToReturn = null;

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Failed to get communicator for instance 'getprop-inst-fail'. Cannot get property.", result.ErrorMessage);
             Assert.IsNull(result.ResultData, "ResultData should be null on error");
        }

        [TestCase("instance_id", "Missing or invalid required parameter: instance_id (string).")]
        [TestCase("element_id", "Missing or invalid required parameter: element_id (string).")]
        [TestCase("property_name", "Missing or invalid required parameter: property_name (string).")]
        public async Task ExecuteAsync_MissingRequiredParameter_ReturnsError(string missingKey, string expectedError)
        {
            // Arrange
            var parameters = CreateValidGetPropertyParams();
            parameters.Remove(missingKey);

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(expectedError, result.ErrorMessage);
             Assert.IsNull(result.ResultData);
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase("instance_id", 123, "Missing or invalid required parameter: instance_id (string).")]
        [TestCase("element_id", true, "Missing or invalid required parameter: element_id (string).")]
        [TestCase("property_name", 99.9, "Missing or invalid required parameter: property_name (string).")]
        public async Task ExecuteAsync_InvalidParameterType_ReturnsError(string key, object invalidValue, string expectedError)
        {
             // Arrange
            var parameters = CreateValidGetPropertyParams();
            parameters[key] = invalidValue;

            // Act
            ToolResult result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(expectedError, result.ErrorMessage);
            Assert.IsNull(result.ResultData);
            LogAssert.NoUnexpectedReceived();
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
            Assert.AreEqual("Parameters dictionary is null.", result.ErrorMessage);
            Assert.IsNull(result.ResultData);
            LogAssert.NoUnexpectedReceived();
        }
    }
} 