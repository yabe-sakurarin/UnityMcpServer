// using System.IO; // System.IO は不要になったため削除しても良いかもしれません
using System.Linq; // Added for FirstOrDefault
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
        // Removed: Stdio mocks
        private JsonSerializerSettings _jsonSettings;
        private const string TestSessionId = "test-session-id"; // Define a constant for session ID used in tests

        [SetUp]
        public void SetUp()
        {
            // Create a GameObject to host the MonoBehaviour
            _handlerObject = new GameObject("MCPHandlerTest");
            _handler = _handlerObject.AddComponent<MCPCommunicationHandler>();

            // Configure the handler for testing
            _handler.EnterTestMode(); // Use the test hook

            // JSON settings for verification
            _jsonSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
        }

        [TearDown]
        public void TearDown()
        {
            // Destroy the GameObject
            if (_handlerObject != null)
            {
                GameObject.DestroyImmediate(_handlerObject);
            }
            // Removed: Stdio mock disposal
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

            // Clear previous messages for this session
            _handler.ClearSentMessagesForSession(TestSessionId);

            // Act: Simulate receiving the request using the test hook
            await _handler.SimulateReceiveMessageAsync(requestJson, TestSessionId);

            // Assert: Check the captured response using the test hook
            var sentMessages = _handler.GetSentMessagesForSession(TestSessionId);
            Assert.IsNotEmpty(sentMessages, "Handler did not produce any output.");
            string responseJson = sentMessages.FirstOrDefault(); // Get the first captured message
            Assert.IsNotNull(responseJson, "Captured message list was unexpectedly empty after check.");

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

            // Clear previous messages
            _handler.ClearSentMessagesForSession(TestSessionId);

            // Act using the test hook
            await _handler.SimulateReceiveMessageAsync(requestJson, TestSessionId);

            // Assert using the test hook
            var sentMessages = _handler.GetSentMessagesForSession(TestSessionId);
            Assert.IsNotEmpty(sentMessages);
            string responseJson = sentMessages.FirstOrDefault();
            Assert.IsNotNull(responseJson);

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

            // Clear previous messages
            _handler.ClearSentMessagesForSession(TestSessionId);

            // Act using the test hook
            await _handler.SimulateReceiveMessageAsync(requestJson, TestSessionId);

            // Assert using the test hook
            var sentMessages = _handler.GetSentMessagesForSession(TestSessionId);
            Assert.IsNotEmpty(sentMessages);
            string responseJson = sentMessages.FirstOrDefault();
            Assert.IsNotNull(responseJson);

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

        // TODO: Add more tests for tools/call, shutdown, notifications, multiple sessions etc.
    }
}