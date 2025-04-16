using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.wait_for_element' MCP tool.
    /// Simulates waiting for a specific UI element to appear in a Unity instance.
    /// </summary>
    public class GameWaitForElementExecutor : IToolExecutor
    {
        public string ToolName => "game.wait_for_element";

        private readonly IUnityInstanceConnector _instanceConnector;

        public GameWaitForElementExecutor(IUnityInstanceConnector instanceConnector)
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
            if (!TryGetIntegerParameter(parameters, "timeout_ms", out long timeoutMs) || timeoutMs <= 0)
            {
                 // Timeout must be positive
                 return ToolResult.Error("Missing or invalid required parameter: timeout_ms (positive integer).");
            }

            // --- Execution ---
            LoggingService.LogInfo($"Executing {ToolName}: Instance='{instanceId}', Element='{elementId}', Timeout={timeoutMs}ms.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection
            // 2. Repeatedly send a query command for the element existence
            // 3. Receive responses, checking for element found or timeout

            // Simulate successful execution or timeout
            try
            {
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                // Simplified check for simulation
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance '{instanceId}'. Cannot simulate wait.");
                }

                // *** Simulation Logic ***
                // In a real scenario, we would poll the instance.
                // Here, we just simulate based on a hypothetical condition or always succeed/fail based on test setup.
                // For this simulation, let's assume it always *finds* the element if connection is okay.
                // To simulate a timeout, the test should configure the mock connector to fail the connection.

                // Simulate some delay related to the timeout, but not the full duration
                await Task.Delay(Math.Min((int)(timeoutMs * 0.1), 500)); // Simulate a short wait
                
                LoggingService.LogInfo($"Simulated {ToolName}: Element '{elementId}' found (or assumed found) on instance '{instanceId}'.");
                // According to basic design, should return { "found": true/false }.
                // For simplicity in simulation, return SimpleSuccess if found, Error if timeout.
                // Since we assume found if connected, return SimpleSuccess here.
                 return ToolResult.SimpleSuccess(); 
                 // If simulating timeout: return ToolResult.Error($"Element '{elementId}' not found within {timeoutMs}ms timeout.");
            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance '{instanceId}': {ex.Message}");
                return ToolResult.Error($"An error occurred during wait execution simulation: {ex.Message}");
            }
        }
        
        // Helper for parsing integer parameters (long) - Copied from GameSwipeExecutor
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