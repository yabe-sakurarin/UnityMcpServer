using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;
using UnityEngine.TestTools;
using UnityEngine;
using System.Text.RegularExpressions;

namespace Sakurarin.UnityMCPServer.Tests.Editor.Tools.Game
{
    // NOTE: This mock is identical to the one in GameClickExecutorTests.
    // Consider moving to a shared location if more executors use it.
    public class MockUnityInstanceConnector_ForInput : IUnityInstanceConnector // Renamed slightly to avoid conflicts if files are open
    {
        public bool ShouldConnectSuccessfully { get; set; } = true;
        public string InstanceIdToConnect { get; set; } = string.Empty;
        public TcpCommunicator CommunicatorToReturn { get; set; } = null;

        public Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000)
        {
            if (ShouldConnectSuccessfully && instanceId == InstanceIdToConnect)
            {
                return Task.FromResult(CommunicatorToReturn);
            }
            else
            {
                return Task.FromResult<TcpCommunicator>(null);
            }
        }

        public void DisconnectInstance(string instanceId) { }
        public Task<bool> SendMessageToInstanceAsync(string instanceId, string message) => Task.FromResult(true);
        public void Dispose() { }
    }

    public class GameInputExecutorTests
    {
        private GameInputExecutor _executor;
        private MockUnityInstanceConnector_ForInput _mockConnector;

        [SetUp]
        public void SetUp()
        {
            _mockConnector = new MockUnityInstanceConnector_ForInput();
            _executor = new GameInputExecutor(_mockConnector);
        }

        [Test]
        public async Task ExecuteAsync_ValidParameters_Success()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-1" },
                { "element_id", "input_username" },
                { "text", "SakuraRin" }
            };
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "test-inst-1";
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError);
            Assert.IsNull(result.ErrorMessage);
            // LogAssert.NoUnexpectedReceived(); // INFO logs are expected, so omit this check
            _mockConnector.CommunicatorToReturn?.Dispose();
        }
        
        [Test]
        public async Task ExecuteAsync_ValidParametersEmptyText_Success()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "test-inst-empty" },
                { "element_id", "input_field" },
                { "text", "" } // Empty string is valid
            };
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "test-inst-empty";
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 10000);

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError);
            Assert.IsNull(result.ErrorMessage);
            // LogAssert.NoUnexpectedReceived(); // INFO logs are expected
            _mockConnector.CommunicatorToReturn?.Dispose();
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "inst-fail" },
                { "element_id", "any_element" },
                { "text", "some text" }
            };
            _mockConnector.ShouldConnectSuccessfully = false;
            _mockConnector.InstanceIdToConnect = "inst-fail";
            _mockConnector.CommunicatorToReturn = null;

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Failed to get communicator for instance 'inst-fail'. Cannot simulate input.", result.ErrorMessage);
            // LogAssert.NoUnexpectedReceived(); // INFO logs are expected
        }

        [Test]
        public async Task ExecuteAsync_MissingInstanceId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "element_id", "id_field" },
                { "text", "text" }
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived(); // No INFO log expected
        }

        [Test]
        public async Task ExecuteAsync_MissingElementId_ReturnsError()
        {
             // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "inst-1" },
                { "text", "text" }
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: element_id (string).", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public async Task ExecuteAsync_MissingText_ReturnsError()
        {
             // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "inst-1" },
                { "element_id", "elem-1" }
                 // text is missing
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: text (string).", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public async Task ExecuteAsync_InvalidInstanceIdType_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", 123 }, // Not a string
                { "element_id", "elem-1" },
                { "text", "text" }
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public async Task ExecuteAsync_InvalidElementIdType_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "inst-ok" },
                { "element_id", false }, // Not a string
                { "text", "text" }
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: element_id (string).", result.ErrorMessage);
            LogAssert.NoUnexpectedReceived();
        }
        
        [Test]
        public async Task ExecuteAsync_InvalidTextType_ReturnsError()
        {
             // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "inst-ok" },
                { "element_id", "elem-1" },
                { "text", 12345 } // Not a string
            };
            // Act
            var result = await _executor.ExecuteAsync(parameters);
            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: text (string).", result.ErrorMessage);
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