using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sakurarin.UnityMcpServer.Runtime.Core; // LoggingService, ConfigurationLoader
using UnityEngine;

namespace Sakurarin.UnityMcpServer.Runtime.Communication
{
    /// <summary>
    /// Handles MCP communication over a specified transport (initially Stdio).
    /// Parses JSON-RPC messages and routes requests.
    /// </summary>
    public class MCPCommunicationHandler : MonoBehaviour
    {
        // Dependencies (consider using Dependency Injection later)
        // private ConfigurationLoader _configLoader; // Removed instance field for static class
        // private ToolDispatcher _toolDispatcher; // Commented out until implemented

        private TextReader _inputReader;
        private TextWriter _outputWriter;
        private JsonSerializerSettings _jsonSettings;
        private CancellationTokenSource _shutdownCts = new CancellationTokenSource();

        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            // Ensure config is loaded (accesses static property which loads if needed)
            var config = ConfigurationLoader.Config;
            LoggingService.LogInfo($"MCP Handler Awake. LogLevel: {config.GetLogLevel()}");

            // Removed: ConfigurationLoader related code (access statically)
            // Removed: ToolDispatcher initialization (commented out field)

            SetupJsonSerializer();
            // SetupStdioTransport(); // Call transport setup separately
            // Default to Stdio if not configured otherwise by test or other means
            if (_inputReader == null || _outputWriter == null)
            {
                SetupStdioTransport();
            }

            LoggingService.LogInfo("MCPCommunicationHandler Awake: Setup complete.");
        }

        private void Start()
        {
            // Start the main listening loop
            LoggingService.LogInfo("MCPCommunicationHandler Start: Starting message loop...");
            _ = MessageLoopAsync(_shutdownCts.Token);
        }

        private void OnDestroy()
        {
            LoggingService.LogInfo("MCPCommunicationHandler OnDestroy: Shutting down...");
            // Signal shutdown
            _shutdownCts?.Cancel();
            _shutdownCts?.Dispose();

            // Close streams (might be handled by application exit anyway)
            // Input/Output streams are usually managed by the OS for Console
        }

        #endregion

        #region Setup

        private void SetupJsonSerializer()
        {
            _jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                // Add converters if needed (e.g., for specific Unity types)
            };
        }

        /// <summary>
        /// Configures the handler to use specific input/output streams (for testing or alternative transports).
        /// </summary>
        /// <param name="reader">The TextReader for input.</param>
        /// <param name="writer">The TextWriter for output.</param>
        public void ConfigureTransport(TextReader reader, TextWriter writer)
        {
            _inputReader = reader ?? throw new ArgumentNullException(nameof(reader));
            _outputWriter = writer ?? throw new ArgumentNullException(nameof(writer));
            LoggingService.LogInfo($"Transport configured to use custom {reader.GetType().Name} and {writer.GetType().Name}.");

             // If MessageLoop was already started with invalid streams, restart it.
             // This requires stopping the previous loop first.
             _shutdownCts?.Cancel(); // Cancel previous loop if running
             _shutdownCts = new CancellationTokenSource(); // Create new token source
             _ = MessageLoopAsync(_shutdownCts.Token); // Start new loop

        }

        private void SetupStdioTransport()
        {
            // Configure console input/output encoding to UTF-8 without BOM
            // This is crucial for correct JSON parsing
            try
            {
                 Console.InputEncoding = new UTF8Encoding(false);
                 Console.OutputEncoding = new UTF8Encoding(false);
                 _inputReader = Console.In;
                 _outputWriter = Console.Out;
                 LoggingService.LogInfo("Using Stdio transport. Ensure parent process handles streams correctly.");
            }
            catch (IOException ex)
            {
                 // This can happen if the process doesn't have a console (e.g., background service)
                 LoggingService.LogError($"Failed to access Console streams for Stdio transport: {ex.Message}. MCP Handler will not function.");
                 // Disable the component or handle error appropriately
                 enabled = false;
            }
             catch (Exception ex) // Catch other potential exceptions during setup
            {
                 LoggingService.LogError($"Unexpected error setting up Stdio transport: {ex.Message}");
                 enabled = false;
            }
        }

        #endregion

        #region Message Loop & Handling

        private async Task MessageLoopAsync(CancellationToken cancellationToken)
        {
            if (_inputReader == null || _outputWriter == null)
            {
                LoggingService.LogError("Input/Output streams are not available. Stopping message loop.");
                return;
            }

            LoggingService.LogDebug("Entering message loop...");
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Read line asynchronously (will block until a line is available or stream closes)
                    string line = await _inputReader.ReadLineAsync().ConfigureAwait(false);

                    if (line == null) // End of stream
                    {
                        LoggingService.LogInfo("Input stream closed. Exiting message loop.");
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue; // Ignore empty lines
                    }

                    LoggingService.LogDebug($"Received line: {line}");

                    // Process the received line in a separate task to avoid blocking the loop
                    _ = Task.Run(() => ProcessMessageAsync(line, cancellationToken), cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                LoggingService.LogInfo("Input stream was disposed. Exiting message loop.");
            }
            catch (IOException ioEx)
            {
                 LoggingService.LogError($"IO Error in message loop: {ioEx.Message}");
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                LoggingService.LogError($"Unexpected error in message loop: {ex.Message}\n{ex.StackTrace}");
            }
            finally
            {
                LoggingService.LogDebug("Exited message loop.");
                // Perform any final cleanup if needed
            }
        }

        private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
        {
            JsonRpcRequest request = null;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(json, _jsonSettings);

                if (request == null || request.Method == null)
                {
                     await SendErrorResponseAsync(null, StandardRpcError.InvalidRequest, "Invalid JSON-RPC request object.").ConfigureAwait(false);
                     return;
                }

                LoggingService.LogInfo($"Processing method: {request.Method}");

                // --- Handle MCP Methods ---
                switch (request.Method)
                {
                    case "initialize":
                        await HandleInitializeRequestAsync(request).ConfigureAwait(false);
                        break;

                    case "tools/call":
                        // TODO: Implement tool call handling
                        await HandleToolCallRequestAsync(request).ConfigureAwait(false);
                        break;

                    case "shutdown":
                         // Handle graceful shutdown requested by client
                         LoggingService.LogInfo("Shutdown request received from client.");
                         // Respond first if it's not a notification
                         if(request.Id != null) {
                              await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, new JObject())).ConfigureAwait(false);
                         }
                         // Trigger shutdown
                         _shutdownCts?.Cancel();
                         // Optionally call Application.Quit() after a short delay or via main thread
                         break;

                    // TODO: Handle other MCP methods (capabilities, resources, etc.)

                    default:
                         LoggingService.LogWarn($"Received unknown method: {request.Method}");
                         if (request.Id != null) // Only send error for requests, not notifications
                         {
                             await SendErrorResponseAsync(request.Id, StandardRpcError.MethodNotFound).ConfigureAwait(false);
                         }
                        break;
                }
            }
            catch (JsonException jsonEx)
            {
                LoggingService.LogError($"JSON Parse Error: {jsonEx.Message}\nOriginal JSON: {json}");
                 await SendErrorResponseAsync(null, StandardRpcError.ParseError).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Internal error processing request: {ex.Message}\n{ex.StackTrace}");
                 // Attempt to send an internal error response if possible
                object requestId = request?.Id; // May be null if parsing failed early
                 if (requestId != null || IsParseOrInvalidRequestErrorRobust(request, json))
                 {
                     await SendErrorResponseAsync(requestId, StandardRpcError.InternalError, "An internal server error occurred.").ConfigureAwait(false);
                 }
            }
        }

        /// <summary>
        /// Handles the 'initialize' request from the MCP client.
        /// </summary>
        private async Task HandleInitializeRequestAsync(JsonRpcRequest request)
        {
            // Basic validation (MCP spec requires params object)
            if (request.Id == null || request.Params == null || !(request.Params is JObject))
            {
                await SendErrorResponseAsync(request.Id, StandardRpcError.InvalidRequest, "'initialize' requires an ID and a params object.").ConfigureAwait(false);
                return;
            }

            // Process initialization parameters (e.g., client info, capabilities)
            // JObject clientParams = (JObject)request.Params;
            // ... process clientParams ...
            LoggingService.LogInfo($"Initialization request received with ID: {request.Id}. Params: {request.Params.ToString()}");

            // Respond with server capabilities
            // TODO: Define actual server capabilities based on loaded tools etc.
            var serverCapabilities = new JObject
            {
                // Example capability
                ["server_version"] = "0.1.0",
                ["supported_tools"] = new JArray("game.click", "log.message") // Placeholder
                // Add more capabilities as defined in MCP spec
            };

            await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, serverCapabilities)).ConfigureAwait(false);
            _isInitialized = true; // Mark server as initialized
            LoggingService.LogInfo("Server initialized successfully.");
        }

         /// <summary>
        /// Placeholder for handling 'tools/call' requests.
        /// </summary>
        private async Task HandleToolCallRequestAsync(JsonRpcRequest request)
        {
            if (!_isInitialized)
            {
                 await SendErrorResponseAsync(request.Id, JsonRpcErrorCode.ServerErrorStart, "Server not initialized.").ConfigureAwait(false);
                 return;
            }

            LoggingService.LogWarn("'tools/call' handling not fully implemented yet.");
            // TODO: Parse params, find appropriate IToolExecutor via ToolDispatcher, execute, and return result/error

            // Placeholder response
            if (request.Id != null)
            {
                // Simulate finding the tool but not running it yet
                 var toolName = ((request.Params as JObject)?["tool_name"] as JValue)?.ToString() ?? "unknown_tool";
                 await SendResponseAsync(JsonRpcResponse.SuccessResponse(request.Id, new JObject { ["message"] = $"Placeholder response for tool: {toolName}" })).ConfigureAwait(false);
                 // Or send an error if dispatcher is not ready
                // await SendErrorResponseAsync(request.Id, StandardRpcError.InternalError, "ToolDispatcher not available.").ConfigureAwait(false);
            }
        }

        #endregion

        #region Sending Responses

        private async Task SendResponseAsync(JsonRpcResponse response)
        {            if (_outputWriter == null) return;
            try
            {
                string jsonResponse = JsonConvert.SerializeObject(response, _jsonSettings);
                LoggingService.LogDebug($"Sending response: {jsonResponse}");
                await _outputWriter.WriteLineAsync(jsonResponse).ConfigureAwait(false);
                 // Ensure flush if AutoFlush is not working reliably in all environments
                 await _outputWriter.FlushAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error sending response: {ex.Message}");
                // Handle potential disconnection or stream errors
            }
        }

        private Task SendErrorResponseAsync(object id, StandardRpcError errorType, string message = null, object data = null)
        {
            return SendResponseAsync(JsonRpcResponse.ErrorResponse(id, errorType, message, data));
        }
        private Task SendErrorResponseAsync(object id, JsonRpcErrorCode errorCode, string message, object data = null)
        {
            return SendResponseAsync(JsonRpcResponse.ErrorResponse(id, errorCode, message, data));
        }

         /// <summary>
        /// Helper to determine if sending an error is appropriate even if request parsing failed.
        /// Aims to send ParseError or InvalidRequest according to JSON-RPC spec recommendations.
        /// </summary>
        private bool IsParseOrInvalidRequestErrorRobust(JsonRpcRequest partiallyParsedRequest, string originalJson)
        {
             // JSON-RPC spec says: "If there was an error in detecting the id in the Request object (e.g. Parse error/Invalid Request),
             // it MUST be Null." - We send ParseError/InvalidRequest with null ID if parsing failed badly.
            if (partiallyParsedRequest == null)
            {
                 // Check if it looks vaguely like JSON to avoid responding to garbage
                 return originalJson.Trim().StartsWith("{") || originalJson.Trim().StartsWith("[");
            }
            // If we parsed something but it's invalid (e.g., missing method), we should have an ID (or null for notification)
             // In this case, send error with the parsed ID (even if null).
             return true;
        }

        #endregion
    }
} 