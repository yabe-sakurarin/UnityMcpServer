using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq; // For JObject/JValue if used for params
using Sakurarin.UnityMcpServer.Runtime.Core;

namespace Sakurarin.UnityMcpServer.Runtime.Tools.Log
{
    /// <summary>
    /// Executes the 'log.message' MCP tool.
    /// </summary>
    public class LogMessageExecutor : IToolExecutor
    {
        public string ToolName => "log.message";

        public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            // --- Parameter Parsing & Validation --- 
            if (parameters == null)
            {
                return Task.FromResult(ToolResult.Error("Parameters dictionary is null."));
            }

            // Get required parameters
            if (!parameters.TryGetValue("instance_id", out var instanceIdObj) || !(instanceIdObj is string instanceId) || string.IsNullOrEmpty(instanceId))
            {
                return Task.FromResult(ToolResult.Error("Missing or invalid required parameter: instance_id (string)."));
            }
            if (!parameters.TryGetValue("level", out var levelObj) || !(levelObj is string levelStr) || string.IsNullOrEmpty(levelStr))
            {
                 return Task.FromResult(ToolResult.Error("Missing or invalid required parameter: level (string)."));
            }
            if (!parameters.TryGetValue("message", out var messageObj))
            {
                return Task.FromResult(ToolResult.Error("Missing required parameter: message (string)."));
            }
            if (!(messageObj is string) && messageObj != null) // Error if it exists but is neither string nor null
            {
                return Task.FromResult(ToolResult.Error("Invalid type for parameter: message (expected string)."));
            }
            string message = messageObj as string ?? string.Empty; // Treat null as empty string for logging

            // Parse log level string
             bool didParse = Enum.TryParse<LogLevel>(levelStr, true, out var logLevel);
             // Log TryParse result using LogInfo to ensure visibility
             LoggingService.LogInfo($"LogMessageExecutor: TryParse for level '{levelStr}' returned: {didParse}, parsed value: {logLevel}");

             // Check if parsing failed OR if the parsed value is not a defined enum member
             if (!didParse || !Enum.IsDefined(typeof(LogLevel), logLevel))
             {
                  LoggingService.LogWarn($"Invalid log level '{levelStr}'. Parsed as: {logLevel}. Defined values: {string.Join(", ", Enum.GetNames(typeof(LogLevel)))}");
                  return Task.FromResult(ToolResult.Error($"Invalid log level specified: '{levelStr}'. Valid levels are: Debug, Info, Warn, Error."));
             }

            // --- Execution --- 
            // For now, just log to the server console, prefixed with the instance ID.
            // TODO: Potentially route log message to the specific game instance in the future?
            string logPrefix = $"[Instance {instanceId}] ";
            LoggingService.Log(logLevel, logPrefix + message);

            // --- Result --- 
            // Return success with data as per design document
            var resultData = new Dictionary<string, object>
            {
                { "success", true }
            };
            return Task.FromResult(ToolResult.SuccessWithData(resultData));
        }
    }
} 