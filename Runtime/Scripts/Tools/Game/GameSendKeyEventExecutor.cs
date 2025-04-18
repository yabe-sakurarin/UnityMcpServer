using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // For IUnityInstanceConnector

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.send_key_event' MCP tool.
    /// </summary>
    public class GameSendKeyEventExecutor : IToolExecutor
    {
        public string ToolName => "game.send_key_event";

        private readonly IUnityInstanceConnector _instanceConnector;

        // Constructor for dependency injection
        public GameSendKeyEventExecutor(IUnityInstanceConnector instanceConnector)
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
            if (!parameters.TryGetValue("key_code", out var keyCodeObj) || !(keyCodeObj is string keyCode) || string.IsNullOrEmpty(keyCode))
            {
                return ToolResult.Error("Missing or invalid required parameter: key_code (string).");
            }

            // --- Execution ---
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', KeyCode='{keyCode}'.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection (e.g., await _instanceConnector.GetOrConnectInstanceAsync(instanceId))
            // 2. Construct a key event command message (define protocol, include keyCode)
            // 3. Send the command (e.g., await communicator.SendMessageAsync(keyEventCommandJson))
            // 4. Optionally wait for a response from the instance and check for success/failure

            // Simulate successful execution for now
            try
            {
                // Simulate placeholder logic - Check if instance *could* be connected
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate sending key event.");
                }

                // Simulate work/delay
                await Task.Delay(5); // Placeholder for actual interaction time (sending key event is fast)
                LoggingService.LogInfo($"Simulated {ToolName} successfully executed on instance '{instanceId}' for KeyCode '{keyCode}'.");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                return ToolResult.Error($"An error occurred during key event execution simulation: {ex.Message}");
            }

            // --- Result ---
            // Basic success indicates the command was sent (or simulated successfully).
            // Actual confirmation might require response from the instance in a real implementation.
            return ToolResult.SimpleSuccess();
        }
    }
}
