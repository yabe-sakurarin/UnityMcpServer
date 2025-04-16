using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.input' MCP tool.
    /// Simulates typing text into a specific UI element in a Unity instance.
    /// </summary>
    public class GameInputExecutor : IToolExecutor
    {
        public string ToolName => "game.input";

        private readonly IUnityInstanceConnector _instanceConnector;

        public GameInputExecutor(IUnityInstanceConnector instanceConnector)
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
            if (!parameters.TryGetValue("element_id", out var elementIdObj) || !(elementIdObj is string elementId) || string.IsNullOrEmpty(elementId))
            {
                return ToolResult.Error("Missing or invalid required parameter: element_id (string).");
            }
            // Allow empty string for text, but require the key to exist and be a string
            if (!parameters.TryGetValue("text", out var textObj) || !(textObj is string text))
            {
                return ToolResult.Error("Missing or invalid required parameter: text (string).");
            }

            // --- Execution ---
            // Mask sensitive text in log? Consider if necessary.
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', Element='{elementId}', Text='{text}'."); // Be mindful of logging sensitive text

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection
            // 2. Construct an input command message (e.g., JSON)
            // 3. Send the command
            // 4. Optionally wait for a response

            // Simulate successful execution
            try
            {
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                // Simplified check for simulation
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate input.");
                }

                // Simulate work/delay
                await Task.Delay(15); // Placeholder for actual interaction time
                LoggingService.LogInfo($"Simulated {ToolName} successfully executed on instance '{instanceId}'.");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                return ToolResult.Error($"An error occurred during input execution simulation: {ex.Message}");
            }

            // --- Result ---
            return ToolResult.SimpleSuccess();
        }
    }
} 