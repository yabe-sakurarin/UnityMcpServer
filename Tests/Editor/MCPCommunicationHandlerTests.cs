using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sakurarin.UnityMcpServer.Runtime.Communication;
using UnityEngine;

namespace Sakurarin.UnityMcpServer.Runtime.Tests.Editor
{
    public class MCPCommunicationHandlerTests
    {
        private MCPCommunicationHandler _handler;
        private GameObject _handlerObject;
        private StringWriter _outputBuffer;
        private StringReader _inputBuffer;
        private JsonSerializerSettings _jsonSettings;

        [SetUp]
        public void SetUp()
        {
            // Create a GameObject to host the MonoBehaviour
            _handlerObject = new GameObject("MCPHandlerTest");
            _handler = _handlerObject.AddComponent<MCPCommunicationHandler>();

            // Prepare mock streams
            _outputBuffer = new StringWriter();
            // Input buffer starts empty, we write to it to simulate input
            _inputBuffer = new StringReader(string.Empty);

            // Configure the handler to use the mock streams
            _handler.ConfigureTransport(_inputBuffer, _outputBuffer);

            // JSON settings for verification
            _jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

             // Reset initialization state if the handler persists between tests (it shouldn't with new GameObject)
             // Accessing private field _isInitialized via reflection is possible but brittle.
             // For simplicity, assume new instance per test.
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy the GameObject
            if (_handlerObject != null)
            {
                GameObject.DestroyImmediate(_handlerObject);
            }
            _outputBuffer?.Dispose();
            _inputBuffer?.Dispose();
        }

        // Helper to simulate sending a line to the handler
        private async Task SimulateInputLine(string jsonLine)
        {
            // Recreate StringReader with new input + newline
             _inputBuffer = new StringReader(jsonLine + "\n");
             _handler.ConfigureTransport(_inputBuffer, _outputBuffer); // Re-configure to use the new reader

            // Give the message loop some time to process. 
            // This is a simplification; a more robust test would wait for specific output.
            await Task.Delay(100); // Adjust delay as needed, might be flaky
        }

         // Helper to read the handler's output
        private string ReadOutput()
        {
            return _outputBuffer.ToString();
        }

        [Test]
        public async Task HandlesInitializeRequestCorrectly()
        {
            // Arrange: Prepare initialize request JSON
            var requestId = "init-123";
            var request = new JsonRpcRequest
            {
                Method = "initialize",
                Params = new JObject { ["client_name"] = "TestClient" },
                Id = requestId
            };
            string requestJson = JsonConvert.SerializeObject(request, _jsonSettings);

            // Act: Simulate sending the request
            await SimulateInputLine(requestJson);

            // Assert: Check the response
            string responseJson = ReadOutput().Trim(); // Read trimmed output
            Assert.IsNotEmpty(responseJson, "Handler did not produce any output.");

            // Deserialize and validate the response
            try
            {
                var response = JsonConvert.DeserializeObject<JsonRpcResponse>(responseJson, _jsonSettings);
                Assert.IsNotNull(response, "Output was not valid JSON-RPC response JSON.");
                Assert.AreEqual("2.0", response.JsonRpcVersion);
                Assert.AreEqual(requestId, response.Id, "Response ID does not match request ID.");
                Assert.IsNull(response.Error, "Response should not contain an error.");
                Assert.IsNotNull(response.Result, "Response should contain a result.");

                // Check capabilities (basic)
                var resultObj = response.Result as JObject;
                Assert.IsNotNull(resultObj, "Result should be a JSON object.");
                Assert.IsTrue(resultObj.ContainsKey("server_version"), "Result should contain 'server_version'.");
                Assert.IsTrue(resultObj.ContainsKey("supported_tools"), "Result should contain 'supported_tools'.");
            }
            catch(JsonException ex)
            {
                 Assert.Fail($"Failed to parse handler output as JSON-RPC response: {ex.Message}\nOutput: {responseJson}");
            }
        }

        [Test]
        public async Task SendsErrorForInvalidInitializeRequest()
        {
            // Arrange: Prepare an invalid initialize request (missing params)
            var requestId = "invalid-init";
            var request = new JsonRpcRequest
            {
                Method = "initialize",
                // Params = null, // Missing params
                Id = requestId
            };
            string requestJson = JsonConvert.SerializeObject(request, _jsonSettings);

            // Act
            await SimulateInputLine(requestJson);

            // Assert
            string responseJson = ReadOutput().Trim();
            Assert.IsNotEmpty(responseJson);

            try
            {
                var response = JsonConvert.DeserializeObject<JsonRpcResponse>(responseJson, _jsonSettings);
                Assert.IsNotNull(response);
                Assert.AreEqual(requestId, response.Id);
                Assert.IsNull(response.Result);
                Assert.IsNotNull(response.Error, "Response should contain an error.");
                Assert.AreEqual((long)JsonRpcErrorCode.InvalidRequest, response.Error.Code, "Error code should be InvalidRequest.");
            }
             catch(JsonException ex)
            {
                 Assert.Fail($"Failed to parse handler output as JSON-RPC response: {ex.Message}\nOutput: {responseJson}");
            }
        }

         [Test]
        public async Task SendsMethodNotFoundErrorForUnknownMethod()
        {
            // Arrange
            var requestId = "unknown-req";
            var request = new JsonRpcRequest
            {
                Method = "some_unknown_method",
                Params = new JObject(),
                Id = requestId
            };
            string requestJson = JsonConvert.SerializeObject(request, _jsonSettings);

            // Act
            await SimulateInputLine(requestJson);

            // Assert
            string responseJson = ReadOutput().Trim();
            Assert.IsNotEmpty(responseJson);
            try
            {
                 var response = JsonConvert.DeserializeObject<JsonRpcResponse>(responseJson, _jsonSettings);
                 Assert.IsNotNull(response);
                 Assert.AreEqual(requestId, response.Id);
                 Assert.IsNull(response.Result);
                 Assert.IsNotNull(response.Error);
                 Assert.AreEqual((long)JsonRpcErrorCode.MethodNotFound, response.Error.Code);
            }
             catch(JsonException ex)
            {
                 Assert.Fail($"Failed to parse handler output as JSON-RPC response: {ex.Message}\nOutput: {responseJson}");
            }
        }
    }
} 