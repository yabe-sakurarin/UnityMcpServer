using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.swipe' MCP tool.
    /// Simulates swiping on the screen of a Unity instance.
    /// </summary>
    public class GameSwipeExecutor : IToolExecutor
    {
        public string ToolName => "game.swipe";

        private readonly IUnityInstanceConnector _instanceConnector;

        public GameSwipeExecutor(IUnityInstanceConnector instanceConnector)
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

            // Get required parameters with type checking
            if (!parameters.TryGetValue("instance_id", out var instanceIdObj) || !(instanceIdObj is string instanceId) || string.IsNullOrEmpty(instanceId))
            {
                return ToolResult.Error("Missing or invalid required parameter: instance_id (string).");
            }

            if (!TryGetNumericParameter(parameters, "start_x", out double startX))
            {
                 return ToolResult.Error("Missing or invalid required parameter: start_x (number).");
            }
            if (!TryGetNumericParameter(parameters, "start_y", out double startY))
            {
                 return ToolResult.Error("Missing or invalid required parameter: start_y (number).");
            }
             if (!TryGetNumericParameter(parameters, "end_x", out double endX))
            {
                 return ToolResult.Error("Missing or invalid required parameter: end_x (number).");
            }
             if (!TryGetNumericParameter(parameters, "end_y", out double endY))
            {
                 return ToolResult.Error("Missing or invalid required parameter: end_y (number).");
            }
             if (!TryGetIntegerParameter(parameters, "duration_ms", out long durationMs) || durationMs <= 0)
            {
                 // Duration must be positive
                 return ToolResult.Error("Missing or invalid required parameter: duration_ms (positive integer).");
            }

            // --- Execution ---
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', Start=({startX},{startY}), End=({endX},{endY}), Duration={durationMs}ms.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection
            // 2. Construct a swipe command message (e.g., JSON with coordinates and duration)
            // 3. Send the command
            // 4. Optionally wait for a response

            // Simulate successful execution
            try
            {
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                // Simplified check for simulation
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate swipe.");
                }

                // Simulate work/delay based on duration
                // Clamp delay to a reasonable max for simulation purpose
                await Task.Delay(Math.Min((int)durationMs, 2000)); 
                LoggingService.LogInfo($"Simulated {ToolName} successfully executed on instance '{instanceId}'.");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                return ToolResult.Error($"An error occurred during swipe execution simulation: {ex.Message}");
            }

            // --- Result ---
            return ToolResult.SimpleSuccess();
        }

        // Helper for parsing numeric parameters (double)
        private bool TryGetNumericParameter(Dictionary<string, object> parameters, string key, out double value)
        {
             value = 0;
             if (!parameters.TryGetValue(key, out var obj)) return false;
             try
             {
                 // Handle potential int/long types coming from JSON/Dictionary
                 value = Convert.ToDouble(obj);
                 return true;
             }
             catch (FormatException) { return false; }
             catch (InvalidCastException) { return false; }
             catch (OverflowException) { return false; }
        }
        
        // Helper for parsing integer parameters (long)
        private bool TryGetIntegerParameter(Dictionary<string, object> parameters, string key, out long value)
        {
            value = 0;
            if (!parameters.TryGetValue(key, out var obj)) return false;
             try
             {
                 value = Convert.ToInt64(obj);
                 return true;
             }
             catch (FormatException) { return false; }
             catch (InvalidCastException) { return false; }
             catch (OverflowException) { return false; }
        }
    }
} 