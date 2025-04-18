using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // For IUnityInstanceConnector
// Potentially needed for script execution result parsing later:
// using Newtonsoft.Json;
// using Newtonsoft.Json.Linq;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.execute_script' MCP tool.
    /// WARNING: This tool carries significant security risks if not implemented carefully.
    /// </summary>
    public class GameExecuteScriptExecutor : IToolExecutor
    {
        public string ToolName => "game.execute_script";

        private readonly IUnityInstanceConnector _instanceConnector;

        // Constructor for dependency injection
        public GameExecuteScriptExecutor(IUnityInstanceConnector instanceConnector)
        {
            _instanceConnector = instanceConnector ?? throw new System.ArgumentNullException(nameof(instanceConnector));
        }

        public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // --- Parameter Parsing & Validation ---
            if (parameters == null)
            {
                return ToolResult.Error("Parameters dictionary is null.");
            }
            if (_instanceConnector == null)
            {
                return ToolResult.Error("UnityInstanceConnector is not available.");
            }

            // Get required parameters
            if (!parameters.TryGetValue("instance_id", out var instanceIdObj) || !(instanceIdObj is string instanceId) || string.IsNullOrEmpty(instanceId))
            {
                return ToolResult.Error("Missing or invalid required parameter: instance_id (string).");
            }
            if (!parameters.TryGetValue("script", out var scriptObj) || !(scriptObj is string scriptContent) || string.IsNullOrEmpty(scriptContent))
            {
                // Allow empty script string? For now, require non-empty.
                return ToolResult.Error("Missing or invalid required parameter: script (string, non-empty).");
            }

            // --- Security Warning ---
            LoggingService.LogWarn($"Executing potentially dangerous tool '{ToolName}' for instance '{instanceId}'. Ensure target instance has proper sandboxing and security measures.");

            // --- Execution ---
            LoggingService.LogInfo($"Attempting to execute script on instance '{instanceId}'. Script starts with: {scriptContent.Substring(0, System.Math.Min(50, scriptContent.Length))}...");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection (e.g., await _instanceConnector.GetOrConnectInstanceAsync(instanceId))
            // 2. Define a communication protocol for script execution. This is CRITICAL and needs careful design.
            //    - How is the script sent? (e.g., as part of a JSON command)
            //    - How is the result (or error) returned from the instance? (e.g., JSON response)
            //    - What are the security limitations on the target instance? (Sandboxing, allowed APIs, etc.)
            // 3. Construct the script execution command message (e.g., JSON).
            // 4. Send the command (e.g., await communicator.SendMessageAsync(executeScriptCommandJson)).
            // 5. Wait for and parse the response from the instance (containing result and/or error).

            // Simulate successful execution for now (returns null result and no error)
            try
            {
                // Simulate placeholder logic - Check if instance *could* be connected
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate script execution.");
                }

                // Simulate work/delay
                await Task.Delay(20); // Placeholder for potential execution time
                LoggingService.LogInfo($"Simulated script execution request sent successfully to instance '{instanceId}'.");

                // --- Result ---
                // Simulate success: Return null result and null error as per design doc example
                var resultData = new Dictionary<string, object>
                {
                    { "result", null }, // Actual result would come from the target instance response
                    { "error", null }   // Actual error would come from the target instance response
                };
                return ToolResult.SuccessWithData(resultData);

            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                // Return error based on simulation failure
                 var errorData = new Dictionary<string, object>
                {
                    { "result", null },
                    { "error", $"An error occurred during script execution simulation: {ex.Message}" }
                };
                // Note: The design doc shows a structure with result/error, not a top-level 'isError'.
                // We return SuccessWithData containing the error string, aligning with the return schema.
                return ToolResult.SuccessWithData(errorData);
                // Alternatively, if a general execution failure should be a ToolResult error:
                // return ToolResult.Error($"An error occurred during script execution simulation: {ex.Message}");
            }
        }
    }
} 