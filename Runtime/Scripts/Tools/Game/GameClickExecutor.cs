using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // Keep for IUnityInstanceConnector
using UnityEngine; // Required for Object.FindObjectOfType if used

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.click' MCP tool.
    /// </summary>
    public class GameClickExecutor : IToolExecutor
    {
        public string ToolName => "game.click";

        // Depend on the interface
        private readonly IUnityInstanceConnector _instanceConnector;

        // Constructor for dependency injection (primary constructor)
        public GameClickExecutor(IUnityInstanceConnector instanceConnector)
        {
            _instanceConnector = instanceConnector ?? throw new System.ArgumentNullException(nameof(instanceConnector));
        }

        // Removed default constructor and FindObjectOfType fallback

        public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // --- Parameter Parsing & Validation --- 
            if (parameters == null)
            {
                return ToolResult.Error("Parameters dictionary is null.");
            }
            if (_instanceConnector == null) // Check if connector is available
            {
                return ToolResult.Error("UnityInstanceConnector is not available.");
            }

            // Get required parameters
            if (!parameters.TryGetValue("instance_id", out var instanceIdObj) || !(instanceIdObj is string instanceId) || string.IsNullOrEmpty(instanceId))
            {
                return ToolResult.Error("Missing or invalid required parameter: instance_id (string).");
            }
            if (!parameters.TryGetValue("element_id", out var elementIdObj) || !(elementIdObj is string elementId) || string.IsNullOrEmpty(elementId))
            {                 return ToolResult.Error("Missing or invalid required parameter: element_id (string).");
            }

            // Optional parameter (example, currently unused)
            // bool waitForStable = true; // Default value
            // if (parameters.TryGetValue("wait_for_stable", out var waitForStableObj) && waitForStableObj is bool stableBool)
            // {
            //     waitForStable = stableBool;
            // }

            // --- Execution --- 
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', Element='{elementId}'.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection (e.g., await _instanceConnector.GetOrConnectInstanceAsync(instanceId))
            // 2. Construct a click command message (define protocol)
            // 3. Send the command (e.g., await communicator.SendMessageAsync(clickCommandJson))
            // 4. Optionally wait for a response from the instance and check for success/failure

            // Simulate successful execution for now
            try
            {
                // Simulate placeholder logic - Check if instance *could* be connected (doesn't send anything yet)
                 var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                 // Simplified check for simulation: Only check if communicator is null
                 // if (communicator == null || !communicator.IsConnected) // Old check
                 if (communicator == null) // Simplified check
                 {
                    // Adjusted error message slightly for clarity in simulation context
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate click.");
                 }

                // Simulate work/delay
                await Task.Delay(10); // Placeholder for actual interaction time
                LoggingService.LogInfo($"Simulated {ToolName} successfully executed on instance '{instanceId}'.");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                return ToolResult.Error($"An error occurred during click execution simulation: {ex.Message}");
            }

            // --- Result --- 
            return ToolResult.SimpleSuccess();
        }
    }
} 