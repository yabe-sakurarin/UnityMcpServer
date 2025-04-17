using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.get_element_property' MCP tool.
    /// Simulates getting a property value from a specific UI element in a Unity instance.
    /// </summary>
    public class GameGetElementPropertyExecutor : IToolExecutor
    {
        public string ToolName => "game.get_element_property";

        private readonly IUnityInstanceConnector _instanceConnector;

        public GameGetElementPropertyExecutor(IUnityInstanceConnector instanceConnector)
        {
            _instanceConnector = instanceConnector ?? throw new ArgumentNullException(nameof(instanceConnector));
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
            if (!parameters.TryGetValue("property_name", out var propertyNameObj) || !(propertyNameObj is string propertyName) || string.IsNullOrEmpty(propertyName))
            {
                // TODO: Optionally validate propertyName against a list of supported properties?
                return ToolResult.Error("Missing or invalid required parameter: property_name (string).");
            }

            // --- Execution ---
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', Element='{elementId}', Property='{propertyName}'.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection
            // 2. Construct a query command message (e.g., JSON with element_id and property_name)
            // 3. Send the command and wait for response
            // 4. Parse the response containing the property value

            // Simulate successful execution or connection failure
            try
            {
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                if (communicator == null)
                {
                    // Return Core.ToolResult using the factory method
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot get property.");
                }

                // *** Simulation Logic ***
                object simulatedValue = $"simulated_value_for_{propertyName}";
                // If propertyName == "visible", return true/false etc.
                // switch(propertyName.ToLower())
                // {
                //     case "text": simulatedValue = "Simulated Text"; break;
                //     case "visible": simulatedValue = true; break;
                //     case "enabled": simulatedValue = true; break;
                //     case "position": simulatedValue = new { x = 10.0, y = 20.5 }; break; // Example object
                //     default: simulatedValue = null; break; // Or return an error for unsupported property
                // }

                LoggingService.LogInfo($"Simulated {ToolName}: Returning simulated value for property '{propertyName}' on instance '{instanceId}'.");

                var resultData = new Dictionary<string, object> { { "value", simulatedValue } };
                // Use simple name now that the conflict is resolved
                return ToolResult.SuccessWithData(resultData); 
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                // Return Core.ToolResult using the factory method
                return ToolResult.Error($"An error occurred during get property execution simulation: {ex.Message}");
            }
        }
    }
} 