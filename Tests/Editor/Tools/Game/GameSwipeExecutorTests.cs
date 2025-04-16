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
    public class MockUnityInstanceConnector_ForSwipe : IUnityInstanceConnector
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

    public class GameSwipeExecutorTests
    {
        private GameSwipeExecutor _executor;
        private MockUnityInstanceConnector_ForSwipe _mockConnector;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector_ForSwipe();
            _executor = new GameSwipeExecutor(_mockConnector);
        }

        private Dictionary<string, object> CreateValidSwipeParams(string instanceId = "swipe-inst-1") => new Dictionary<string, object>
        {
            { "instance_id", instanceId },
            { "start_x", 100.0 }, // Use double for coordinates
            { "start_y", 200.5 },
            { "end_x", 300.0 },
            { "end_y", 400.8 },
            { "duration_ms", 500 } // Use int or long for duration
        };

        [Test]
        public async Task ExecuteAsync_ValidParameters_Success()
        {
            // Arrange
            var parameters = CreateValidSwipeParams("swipe-inst-ok");
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "swipe-inst-ok";
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Expected success but got error: {result.ErrorMessage}");
            Assert.IsNull(result.ErrorMessage);
            _mockConnector.CommunicatorToReturn?.Dispose();
        }
        
        [Test]
        public async Task ExecuteAsync_ValidParametersIntegerCoords_Success()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "int-coords" },
                { "start_x", 100 }, // Integer coordinates should also work
                { "start_y", 200 },
                { "end_x", 300 },
                { "end_y", 400 },
                { "duration_ms", 500L } // Long duration
            };
             _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "int-coords";
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError, $"Expected success with int coords but got error: {result.ErrorMessage}");
            Assert.IsNull(result.ErrorMessage);
            _mockConnector.CommunicatorToReturn?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            var parameters = CreateValidSwipeParams("swipe-inst-fail");
            _mockConnector.ShouldConnectSuccessfully = false;
            _mockConnector.InstanceIdToConnect = "swipe-inst-fail";
            _mockConnector.CommunicatorToReturn = null;

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Failed to get communicator for instance 'swipe-inst-fail'. Cannot simulate swipe.", result.ErrorMessage);
        }

        [TestCase("instance_id", "Missing or invalid required parameter: instance_id (string).")]
        [TestCase("start_x", "Missing or invalid required parameter: start_x (number).")]
        [TestCase("start_y", "Missing or invalid required parameter: start_y (number).")]
        [TestCase("end_x", "Missing or invalid required parameter: end_x (number).")]
        [TestCase("end_y", "Missing or invalid required parameter: end_y (number).")]
        [TestCase("duration_ms", "Missing or invalid required parameter: duration_ms (positive integer).")]
        public async Task ExecuteAsync_MissingRequiredParameter_ReturnsError(string missingKey, string expectedError)
        {
            // Arrange
            var parameters = CreateValidSwipeParams();
            parameters.Remove(missingKey);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual(expectedError, result.ErrorMessage);
             LogAssert.NoUnexpectedReceived();
        }

        [TestCase("start_x", "not_a_number", "Missing or invalid required parameter: start_x (number).")]
        [TestCase("duration_ms", "not_an_int", "Missing or invalid required parameter: duration_ms (positive integer).")]
        [TestCase("duration_ms", -100, "Missing or invalid required parameter: duration_ms (positive integer).")] // Negative duration
         [TestCase("duration_ms", 0, "Missing or invalid required parameter: duration_ms (positive integer).")] // Zero duration
        public async Task ExecuteAsync_InvalidParameterTypeOrValue_ReturnsError(string key, object invalidValue, string expectedError)
        {
             // Arrange
            var parameters = CreateValidSwipeParams();
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