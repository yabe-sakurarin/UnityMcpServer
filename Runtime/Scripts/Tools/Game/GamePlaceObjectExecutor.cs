using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection; // For IUnityInstanceConnector
using Newtonsoft.Json.Linq; // For JObject parsing
using UnityEngine; // For Vector3 and Quaternion

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Game
{
    /// <summary>
    /// Executes the 'game.place_object' MCP tool.
    /// </summary>
    public class GamePlaceObjectExecutor : IToolExecutor
    {
        public string ToolName => "game.place_object";

        private readonly IUnityInstanceConnector _instanceConnector;

        // Constructor for dependency injection
        public GamePlaceObjectExecutor(IUnityInstanceConnector instanceConnector)
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
            if (!parameters.TryGetValue("object_id", out var objectIdObj) || !(objectIdObj is string objectId) || string.IsNullOrEmpty(objectId))
            {
                return ToolResult.Error("Missing or invalid required parameter: object_id (string).");
            }

            // Get and validate 'position' parameter
            Vector3 position;
            if (!parameters.TryGetValue("position", out var positionObj) || !(positionObj is JObject positionJObj))
            {
                 return ToolResult.Error("Missing or invalid required parameter: position (object with x, y, z).");
            }
            try
            {
                 // Refactored parsing logic
                 float x, y, z;

                 if (!positionJObj.TryGetValue("x", out var xToken) || !(xToken is JValue xValue) || (xValue.Type != JTokenType.Float && xValue.Type != JTokenType.Integer))
                 {
                     return ToolResult.Error("Invalid or missing 'x' in position parameter.");
                 }
                 x = xValue.Value<float>();

                 if (!positionJObj.TryGetValue("y", out var yToken) || !(yToken is JValue yValue) || (yValue.Type != JTokenType.Float && yValue.Type != JTokenType.Integer))
                 {
                     return ToolResult.Error("Invalid or missing 'y' in position parameter.");
                 }
                 y = yValue.Value<float>();

                 if (!positionJObj.TryGetValue("z", out var zToken) || !(zToken is JValue zValue) || (zValue.Type != JTokenType.Float && zValue.Type != JTokenType.Integer))
                 {
                     return ToolResult.Error("Invalid or missing 'z' in position parameter.");
                 }
                 z = zValue.Value<float>();

                 position = new Vector3(x, y, z);
            }
            catch (System.Exception ex)
            {
                 // Catch potential exceptions during value conversion (though type checks should prevent most)
                 return ToolResult.Error($"Failed to parse 'position' parameter components: {ex.Message}");
            }


            // Get optional 'rotation' parameter, default to identity
            Quaternion rotation = Quaternion.identity; // Default rotation
             if (parameters.TryGetValue("rotation", out var rotationObj) && rotationObj is JObject rotationJObj)
             {
                try
                {
                    // Use JObject's TryGetValue and explicit casting/conversion
                    if (rotationJObj.TryGetValue("x", out var xRotToken) && rotationJObj.TryGetValue("y", out var yRotToken) &&
                        rotationJObj.TryGetValue("z", out var zRotToken) && rotationJObj.TryGetValue("w", out var wRotToken) &&
                        (xRotToken is JValue xRotVal && (xRotVal.Type == JTokenType.Float || xRotVal.Type == JTokenType.Integer)) &&
                        (yRotToken is JValue yRotVal && (yRotVal.Type == JTokenType.Float || yRotVal.Type == JTokenType.Integer)) &&
                        (zRotToken is JValue zRotVal && (zRotVal.Type == JTokenType.Float || zRotVal.Type == JTokenType.Integer)) &&
                        (wRotToken is JValue wRotVal && (wRotVal.Type == JTokenType.Float || wRotVal.Type == JTokenType.Integer)))
                    {
                         rotation = new Quaternion(xRotVal.Value<float>(), yRotVal.Value<float>(), zRotVal.Value<float>(), wRotVal.Value<float>());
                    }
                    else
                    {
                         // Log a warning if rotation object exists but is malformed, use default
                         LoggingService.LogWarn($"Optional parameter 'rotation' provided but malformed. Using default rotation. Provided: {rotationJObj.ToString()}");
                    }
                }
                catch (System.Exception ex)
                {
                    LoggingService.LogWarn($"Failed to parse optional 'rotation' parameter: {ex.Message}. Using default rotation.");
                    // Keep default rotation
                }
             }


            // --- Execution ---
            LoggingService.LogInfo($"Executing {ToolName}: Instance=\'{instanceId}\', ObjectId=\'{objectId}\', Position=\'{position}\', Rotation=\'{rotation}\'.");

            // TODO: Implement actual communication with the Unity Instance via _instanceConnector
            // 1. Get the specific instance connection (e.g., await _instanceConnector.GetOrConnectInstanceAsync(instanceId))
            // 2. Construct an object placement command message (define protocol, include objectId, position, rotation)
            // 3. Send the command (e.g., await communicator.SendMessageAsync(placeObjectCommandJson))
            // 4. Optionally wait for a response from the instance (e.g., containing the placed object's instance ID or an error)

            // Simulate successful execution for now
            try
            {
                // Simulate placeholder logic - Check if instance *could* be connected
                var communicator = await _instanceConnector.GetOrConnectInstanceAsync(instanceId);
                if (communicator == null)
                {
                    return ToolResult.Error($"Failed to get communicator for instance \'{instanceId}\'. Cannot simulate placing object.");
                }

                // Simulate work/delay
                await Task.Delay(15); // Placeholder for actual interaction time
                LoggingService.LogInfo($"Simulated {ToolName} successfully executed on instance \'{instanceId}\' for ObjectId \'{objectId}\'.");

                 // --- Result ---
                // Simulate success: Return success=true and null for placed_object_instance_id as per design doc example
                var resultData = new Dictionary<string, object>
                {
                    { "success", true },
                    { "placed_object_instance_id", null } // Actual ID would come from the target instance response
                };
                 return ToolResult.SuccessWithData(resultData);

            }
            catch (System.Exception ex)
            {
                LoggingService.LogError($"Error during {ToolName} execution simulation for instance \'{instanceId}\': {ex.Message}");
                return ToolResult.Error($"An error occurred during object placement execution simulation: {ex.Message}");
            }
        }
    }
} 