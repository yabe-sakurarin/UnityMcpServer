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
    public class MockUnityInstanceConnector_ForWait : IUnityInstanceConnector
    {
        public bool ShouldConnectSuccessfully { get; set; } = true;
        public string InstanceIdToConnect { get; set; } = string.Empty;
        public TcpCommunicator CommunicatorToReturn { get; set; } = null;
        // We don't need a specific flag for element found in this simplified simulation
        // Success/Failure is determined by ShouldConnectSuccessfully

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

    public class GameWaitForElementExecutorTests
    {
        private GameWaitForElementExecutor _executor;
        private MockUnityInstanceConnector_ForWait _mockConnector;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector_ForWait();
            _executor = new GameWaitForElementExecutor(_mockConnector);
        }

        private Dictionary<string, object> CreateValidWaitParams(string instanceId = "wait-inst-1") => new Dictionary<string, object>
        {
            { "instance_id", instanceId },
            { "element_id", "button_login" },
            { "timeout_ms", 5000 }
        };

        [Test]
        public async Task ExecuteAsync_ValidParametersAndConnection_Success()
        {
            // Arrange
            var parameters = CreateValidWaitParams("wait-inst-ok");
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "wait-inst-ok";
            // Need a non-null communicator for success
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999); 

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Expected success but got error: {result.ErrorMessage}");
            Assert.IsNull(result.ErrorMessage);
             _mockConnector.CommunicatorToReturn?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidWaitParams("wait-inst-fail");
            _mockConnector.ShouldConnectSuccessfully = false;
            _mockConnector.InstanceIdToConnect = "wait-inst-fail";
            _mockConnector.CommunicatorToReturn = null;

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Failed to get communicator for instance 'wait-inst-fail'. Cannot simulate wait.", result.ErrorMessage);
            // INFO log is expected before error, so don't check NoUnexpectedReceived directly here unless ignoring INFO logs
        }

        [TestCase("instance_id", "Missing or invalid required parameter: instance_id (string).")]
        [TestCase("element_id", "Missing or invalid required parameter: element_id (string).")]
        [TestCase("timeout_ms", "Missing or invalid required parameter: timeout_ms (positive integer).")]
        public async Task ExecuteAsync_MissingRequiredParameter_ReturnsError(string missingKey, string expectedError)
        {
            // Arrange
            var parameters = CreateValidWaitParams();
            parameters.Remove(missingKey);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(expectedError, result.ErrorMessage);
             LogAssert.NoUnexpectedReceived();
        }

        [TestCase("timeout_ms", "not_an_int", "Missing or invalid required parameter: timeout_ms (positive integer).")]
        [TestCase("timeout_ms", -100, "Missing or invalid required parameter: timeout_ms (positive integer).")] // Negative timeout
         [TestCase("timeout_ms", 0, "Missing or invalid required parameter: timeout_ms (positive integer).")] // Zero timeout
        public async Task ExecuteAsync_InvalidParameterTypeOrValue_ReturnsError(string key, object invalidValue, string expectedError)
        {
             // Arrange
            var parameters = CreateValidWaitParams();
            parameters[key] = invalidValue;

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(expectedError, result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public async Task ExecuteAsync_NullParameters_ReturnsError()
        {
            // Arrange
            Dictionary<string, object> parameters = null;
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Parameters dictionary is null.", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }
    }
} 