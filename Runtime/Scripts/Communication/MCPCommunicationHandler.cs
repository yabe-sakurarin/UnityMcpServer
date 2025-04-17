using System;
using System.Collections.Concurrent; // Added for session management
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sakurarin.UnityMcpServer.Runtime.Core; // LoggingService, ConfigurationLoader
using UnityEngine;
using WebSocketSharp; // Added for WebSocket
using WebSocketSharp.Server; // Added for WebSocket Server
using WebSocketSharp.Net; // Added for WebSocketState potentially
using WebSocketSharp.Net.WebSockets; // Added for WebSocketContext

#if UNITY_EDITOR
using System.Collections.Generic; // For testing
#endif

namespace Sakurarin.UnityMcpServer.Runtime.Communication
{
    /// <summary>
    /// Handles MCP communication over WebSocket (previously Stdio).
    /// Parses JSON-RPC messages and routes requests via WebSocket connections.
    /// </summary>
    public class MCPCommunicationHandler : MonoBehaviour
    {
        // --- WebSocket Specific ---
        private WebSocketServer _webSocketServer;
        // TODO: Load port from ConfigurationLoader.Config
        private int _webSocketPort = 9998; // Default port
        private const string ServicePath = "/mcp"; // Path for the MCP WebSocket service

        // --- Common MCP Logic ---
        // Dependencies (consider using Dependency Injection later)
        // private ToolDispatcher _toolDispatcher; // Commented out until implemented
        private JsonSerializerSettings _jsonSettings;
        private bool _isInitialized = false;
        private CancellationTokenSource _appShutdownCts = new CancellationTokenSource(); // For triggering app quit if needed

#if UNITY_EDITOR
        // --- Test Hooks (Editor Only) ---
        public bool IsTesting { get; private set; } = false;
        private readonly ConcurrentDictionary<string, List<string>> _testSentMessages = new ConcurrentDictionary<string, List<string>>();

        /// <summary>
        /// [EDITOR ONLY] Configures the handler for testing without a real WebSocket server.
        /// </summary>
        public void EnterTestMode()
        {
            IsTesting = true;
            _testSentMessages.Clear();
            // Stop the real server if it was running
            _webSocketServer?.Stop();
            _webSocketServer = null;
            LoggingService.LogWarn("MCPCommunicationHandler entered Test Mode. WebSocket server disabled.");
        }

        /// <summary>
        /// [EDITOR ONLY] Simulates receiving a message from a specific client session.
        /// </summary>
        public async Task SimulateReceiveMessageAsync(string message, string sessionId = "test-session-id")
        {
            if (!IsTesting)
            {
                LoggingService.LogError("SimulateReceiveMessageAsync called outside of test mode.");
                return;
            }
            LoggingService.LogDebug($"[Test Hook] Simulating message from {sessionId}: {message}");
            await ProcessMessageAsync(message, sessionId, _appShutdownCts?.Token ?? CancellationToken.None);
        }

        /// <summary>
        /// [EDITOR ONLY] Retrieves all messages sent to a specific session during testing.
        /// </summary>
        public List<string> GetSentMessagesForSession(string sessionId = "test-session-id")
        {
            if (!IsTesting)
            {
                LoggingService.LogError("GetSentMessagesForSession called outside of test mode.");
                return new List<string>();
            }
            return _testSentMessages.TryGetValue(sessionId, out var messages) ? new List<string>(messages) : new List<string>();
        }

        /// <summary>
        /// [EDITOR ONLY] Clears recorded sent messages for a specific session.
        /// </summary>
        public void ClearSentMessagesForSession(string sessionId = "test-session-id")
        {
             _testSentMessages.TryRemove(sessionId, out _);
        }
#endif

        #region WebSocket Behavior Inner Class

        /// <summary>
        /// Handles individual WebSocket connections (/mcp path).
        /// </summary>
        public class MCPWebSocketBehavior : WebSocketBehavior
        {
            private MCPCommunicationHandler _handler;
            // Static dictionary to easily find sessions for sending responses
            private static ConcurrentDictionary<string, MCPWebSocketBehavior> _sessions = new ConcurrentDictionary<string, MCPWebSocketBehavior>();

            // Called by the handler to set the reference
            public void Initialize(MCPCommunicationHandler handler)
            {
                _handler = handler;
            }

            protected override void OnOpen()
            {
                LoggingService.LogInfo($"WebSocket Client Connected: {ID} from {UserEndPoint}");
                _sessions.TryAdd(ID, this);
                // TODO: Consider security checks here (e.g., origin validation)
            }

            protected override void OnMessage(MessageEventArgs e)
            {
                if (_handler == null)
                {
                     LoggingService.LogError($"Handler not initialized for WebSocket session {ID}. Message ignored.");
                     return;
                }
                 if (!e.IsText) {
                     LoggingService.LogWarn($"Received non-text WebSocket message from {ID}. Ignoring.");
                     return; // Only process text messages (JSON)
                 }

                LoggingService.LogDebug($"WebSocket Message Received from {ID}: {e.Data}");

                // Process asynchronously on a thread pool thread
                // Pass the session ID so the handler knows who to respond to
                _ = Task.Run(() => _handler.ProcessMessageAsync(e.Data, ID, _handler._appShutdownCts.Token));
            }

            protected override void OnError(WebSocketSharp.ErrorEventArgs e)
            {
                LoggingService.LogError($"WebSocket Error from {ID}: {e.Message}");
                // Attempt to remove session on error
                _sessions.TryRemove(ID, out _);
            }

            protected override void OnClose(CloseEventArgs e)
            {
                LoggingService.LogInfo($"WebSocket Client Disconnected: {ID}, Code: {e.Code}, Reason: {e.Reason}");
                _sessions.TryRemove(ID, out _);
                 // Optionally notify the handler if needed (e.g., client disconnected during operation)
            }

            /// <summary>
            /// Sends a message (JSON string) back to this specific client.
            /// Ensures it runs on the main thread if interacting with Unity APIs is needed later,
            /// but for now, direct send is fine.
            /// </summary>
            public void SendMessage(string message)
            {
                 // Check state before sending
                 if (ReadyState == WebSocketState.Open)
                 {
                     // Send is synchronous in WebSocketSharp, fine for now
                     Send(message);
                     LoggingService.LogDebug($"Sent WebSocket message to {ID}: {message}");
                 }
                 else
                 {
                      LoggingService.LogWarn($"Attempted to send message to non-open WebSocket session {ID} (State: {ReadyState}). Message: {message}");
                 }
            }

             /// <summary>
             /// Tries to get an active session by its ID.
             /// </summary>
             public static bool TryGetSession(string sessionId, out MCPWebSocketBehavior session)
             {
                 return _sessions.TryGetValue(sessionId, out session);
             }

             /// <summary>
             /// Broadcasts a message to all connected clients.
             /// </summary>
             public static void Broadcast(string message)
             {
                  LoggingService.LogDebug($"Broadcasting message to {_sessions.Count} sessions: {message}");
                  foreach(var session in _sessions.Values)
                  {
                       session.SendMessage(message); // Uses the instance method
                  }
             }
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            var config = ConfigurationLoader.Config;
            LoggingService.LogInfo($"MCP Handler Awake. LogLevel: {config.GetLogLevel()}");
            // TODO: Load _webSocketPort from config if specified

            SetupJsonSerializer();
            SetupWebSocketTransport(); // Setup WebSocket instead of Stdio

            LoggingService.LogInfo("MCPCommunicationHandler Awake: Setup complete.");
        }

        private void Start()
        {
            // Start the WebSocket server
            if (_webSocketServer != null && !_webSocketServer.IsListening)
            {
                 LoggingService.LogInfo($"Starting WebSocket server on port {_webSocketPort}...");
                 _webSocketServer.Start();
                 if (!_webSocketServer.IsListening)
                 {
                     LoggingService.LogError("WebSocket server failed to start listening!");
                 }
            }
        }

        private void OnDestroy()
        {
            LoggingService.LogInfo("MCPCommunicationHandler OnDestroy: Stopping WebSocket server...");
             _appShutdownCts?.Cancel(); // Signal any ongoing tasks relying on this token
             _appShutdownCts?.Dispose();

            // Stop the WebSocket server gracefully
            _webSocketServer?.Stop(); // This closes all connections
            _webSocketServer = null; // Release reference

            LoggingService.LogInfo("WebSocket server stopped.");
        }

        #endregion

        #region Setup

        private void SetupJsonSerializer()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
            };
        }

         private void SetupWebSocketTransport()
         {
#if UNITY_EDITOR
             if (IsTesting) return; // Don't setup real server in test mode
#endif
             try
             {
                 _webSocketServer = new WebSocketServer($"ws://0.0.0.0:{_webSocketPort}");
                 // Add the behavior for the specific MCP path
                 _webSocketServer.AddWebSocketService<MCPWebSocketBehavior>(ServicePath, behavior =>
                 {
                     // Initialize the behavior with a reference to this handler
                     behavior.Initialize(this);
                 });

                 LoggingService.LogInfo($"WebSocket transport configured. Server will listen on ws://<ip>:{_webSocketPort}{ServicePath}");

                 // Removed: WebSocketServer does not have a public OnError event.
                 // Error handling for startup is done via try-catch.
                 // _webSocketServer.OnError += (sender, e) => {
                 //     LoggingService.LogError($"WebSocket Server Error: {e.Message}");
                 // };
             }
             catch (Exception ex)
             {
                  // Corrected: Use LogError instead of LogCritical
                  LoggingService.LogError($"FATAL: Failed to initialize WebSocket server on port {_webSocketPort}: {ex.Message}\n{ex.StackTrace}");
                  // Disable the component or throw to prevent inconsistent state
                  enabled = false;
                  _webSocketServer = null; // Ensure it's null if setup failed
             }
         }

        #endregion

        #region Message Handling (Processing Logic)

        // Renamed: Now takes session ID, removed cancellation token reliance for main loop logic
        /// <summary>
        /// Processes a received JSON message from a specific WebSocket client.
        /// </summary>
        /// <param name="json">The raw JSON string received.</param>
        /// <param name="sessionId">The unique ID of the WebSocket session that sent the message.</param>
        /// <param name="cancellationToken">Token to observe for application shutdown.</param>
        private async Task ProcessMessageAsync(string json, string sessionId, CancellationToken cancellationToken)
        {
            JsonRpcRequest request = null;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(json, _jsonSettings);

                if (request == null || request.Method == null)
                {
                     await SendErrorResponseAsync(null, sessionId, StandardRpcError.InvalidRequest, "Invalid JSON-RPC request object.").ConfigureAwait(false);
                     return;
                }

                // Check for cancellation before processing long tasks (if any)
                if (cancellationToken.IsCancellationRequested) return;

                LoggingService.LogInfo($"Processing method '{request.Method}' from session {sessionId}");

                // --- Handle MCP Methods ---
                switch (request.Method)
                {
                    case "initialize":
                        await HandleInitializeRequestAsync(request, sessionId).ConfigureAwait(false);
                        break;

                    case "tools/call":
                        await HandleToolCallRequestAsync(request, sessionId).ConfigureAwait(false);
                        break;

                    case "shutdown":
                         LoggingService.LogInfo($"Shutdown request received from client {sessionId}.");
                         if(request.Id != null) {
                              await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, new JObject()), sessionId).ConfigureAwait(false);
                         }
                         // Optionally trigger application quit (might need main thread execution)
                         LoggingService.LogWarn("Application Quit via 'shutdown' request not implemented yet.");
                         // UnityMainThreadDispatcher.Instance().Enqueue(() => Application.Quit()); // Requires a main thread dispatcher
                         // OR just signal the handler to stop?
                          _appShutdownCts?.Cancel(); // Signal shutdown if needed elsewhere
                         break;

                    default:
                         LoggingService.LogWarn($"Received unknown method '{request.Method}' from session {sessionId}");
                         if (request.Id != null) // Only send error for requests, not notifications
                         {
                             await SendErrorResponseAsync(request.Id, sessionId, StandardRpcError.MethodNotFound).ConfigureAwait(false);
                         }
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                LoggingService.LogError($"JSON Parse Error from session {sessionId}: {jsonEx.Message}\nOriginal JSON: {json}");
                 await SendErrorResponseAsync(null, sessionId, StandardRpcError.ParseError).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                 LoggingService.LogError($"Internal error processing request from session {sessionId}: {ex.Message}\n{ex.StackTrace}\nOriginal JSON: {json}");
                object requestId = request?.Id;
                 // Send internal error only if we could parse the ID or it was likely a parse/invalid request error
                 bool canDetermineId = requestId != null || (request == null && (json.Trim().StartsWith("{") || json.Trim().StartsWith("[")));
                 if (canDetermineId)
                 {
                     await SendErrorResponseAsync(requestId, sessionId, StandardRpcError.InternalError, "An internal server error occurred.").ConfigureAwait(false);
                 }
            }
        }

        /// <summary>
        /// Handles the 'initialize' request from the MCP client.
        /// </summary>
        private async Task HandleInitializeRequestAsync(JsonRpcRequest request, string sessionId)
        {
            if (request.Id == null || request.Params == null || !(request.Params is JObject))
            {
                await SendErrorResponseAsync(request.Id, sessionId, StandardRpcError.InvalidRequest, "'initialize' requires an ID and a params object.").ConfigureAwait(false);
                return;
            }

            LoggingService.LogInfo($"Initialization request from session {sessionId}. Params: {request.Params.ToString()}");

            var serverCapabilities = new JObject
            {
                ["server_version"] = "0.1.0-ws", // Indicate WebSocket version
                ["supported_tools"] = new JArray("game.click", "log.message") // Placeholder
            };

            await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, serverCapabilities), sessionId).ConfigureAwait(false);
            _isInitialized = true; // Mark server as initialized (globally for now)
            // TODO: Consider per-session initialization state if needed
            LoggingService.LogInfo($"Server initialized for session {sessionId}.");
        }

         /// <summary>
        /// Placeholder for handling 'tools/call' requests.
        /// </summary>
        private async Task HandleToolCallRequestAsync(JsonRpcRequest request, string sessionId)
        {
            // TODO: Potentially check per-session initialization if implemented
            if (!_isInitialized)
            {
                 await SendErrorResponseAsync(request.Id, sessionId, JsonRpcErrorCode.ServerErrorStart - 1, "Server not initialized.").ConfigureAwait(false); // Custom code?
                 return;
            }

             // Basic validation
             if (request.Params == null || !(request.Params is JObject))
             {
                 await SendErrorResponseAsync(request.Id, sessionId, StandardRpcError.InvalidParams, "'tools/call' requires a params object.").ConfigureAwait(false);
                 return;
             }

             var callParams = (JObject)request.Params;
             var toolName = callParams["tool_name"]?.ToString();
             var toolParameters = callParams["parameters"] as JObject; // Assuming parameters is an object

             if (string.IsNullOrEmpty(toolName))
             {
                  await SendErrorResponseAsync(request.Id, sessionId, StandardRpcError.InvalidParams, "'tool_name' is missing in tools/call params.").ConfigureAwait(false);
                  return;
             }

            LoggingService.LogWarn($"'tools/call' handling not fully implemented yet for tool '{toolName}' from session {sessionId}. Params: {toolParameters?.ToString() ?? "null"}");
            // TODO:
            // 1. Find appropriate IToolExecutor via ToolDispatcher (needs implementation) based on toolName.
            // 2. Extract 'instance_id' and other tool-specific parameters from toolParameters.
            // 3. Validate parameters against the tool's schema.
            // 4. Use UnityInstanceConnector to get the communicator for the target instance_id.
            // 5. Execute the tool logic via the communicator.
            // 6. Construct ToolResult (success or error) based on execution outcome.
            // 7. Send the ToolResult back to the client using SendResponseAsync.

            // Placeholder response
            if (request.Id != null)
            {
                // Simulate success for now
                 var placeholderResult = new JObject {
                      ["result"] = new JObject { ["message"] = $"Placeholder SUCCESS response for tool: {toolName}" },
                      ["isError"] = false,
                      ["content"] = new JArray() // Empty content for success
                 };
                 await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, placeholderResult), sessionId).ConfigureAwait(false);

                 // Example Error simulation:
                 // var errorResult = new JObject {
                 //     ["result"] = null, // Or potentially some partial result data
                 //     ["isError"] = true,
                 //     ["content"] = new JArray(new JObject {
                 //         ["type"] = "text",
                 //         ["text"] = $"Placeholder ERROR executing tool: {toolName}"
                 //     })
                 // };
                 // await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, errorResult), sessionId).ConfigureAwait(false); // MCP tool errors are successes at RPC level
            }
        }

        #endregion

        #region Sending Responses (WebSocket)

        /// <summary>
        /// Sends a JSON-RPC response object to a specific WebSocket client session.
        /// </summary>
        private Task SendResponseAsync(JsonRpcResponse response, string sessionId)
        {
#if UNITY_EDITOR
            // In test mode, capture the message instead of sending
            if (IsTesting)
            {
                try
                {
                    string jsonResponse = JsonConvert.SerializeObject(response, _jsonSettings);
                    var messageList = _testSentMessages.GetOrAdd(sessionId, (_) => new List<string>());
                    lock (messageList) // Lock list for thread safety
                    {
                         messageList.Add(jsonResponse);
                    }
                    LoggingService.LogDebug($"[Test Hook] Captured message for {sessionId}: {jsonResponse}");
                }
                catch (Exception ex)
                {
                     LoggingService.LogError($"[Test Hook] Error capturing/serializing test response for {sessionId}: {ex.Message}");
                }
                return Task.CompletedTask;
            }
#endif

             if (MCPWebSocketBehavior.TryGetSession(sessionId, out var session))
             {
                 try
                 {
                     string jsonResponse = JsonConvert.SerializeObject(response, _jsonSettings);
                     session.SendMessage(jsonResponse); // Send via the specific behavior instance
                 }
                 catch (Exception ex)
                 {
                     LoggingService.LogError($"Error serializing or sending response to session {sessionId}: {ex.Message}\nResponse: {response}");
                 }
             }
             else
             {
                  LoggingService.LogWarn($"Cannot send response: WebSocket session not found for ID {sessionId}. Response: {JsonConvert.SerializeObject(response)}");
             }
             return Task.CompletedTask; // Sending is synchronous in the behavior for now
        }

        /// <summary>
        /// Sends a JSON-RPC error response to a specific session using standard error codes.
        /// </summary>
        private Task SendErrorResponseAsync(object id, string sessionId, StandardRpcError errorType, string message = null, object data = null)
        {
            return SendResponseAsync(JsonRpcResponse.ErrorResponse(id, errorType, message, data), sessionId);
        }

        /// <summary>
        /// Sends a JSON-RPC error response to a specific session using custom error codes.
        /// </summary>
        private Task SendErrorResponseAsync(object id, string sessionId, JsonRpcErrorCode errorCode, string message, object data = null)
        {
            return SendResponseAsync(JsonRpcResponse.ErrorResponse(id, errorCode, message, data), sessionId);
        }

        #endregion

        #region Removed Stdio/Helper Methods
        // Removed ConfigureTransport(TextReader, TextWriter)
        // Removed SetupStdioTransport()
        // Removed MessageLoopAsync(CancellationToken)
        // Removed IsParseOrInvalidRequestErrorRobust(JsonRpcRequest, string) - simplified check in ProcessMessageAsync
        #endregion
    }
} 