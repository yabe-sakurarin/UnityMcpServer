using Sakurarin.UnityMcpServer.Runtime.Core;
using UnityEngine;

public class SampleScript : MonoBehaviour
{
    void Start()
    {
        // Ensure config is loaded (will load defaults if file not found)
        // ConfigurationLoader.LoadConfig(); // Explicit load (optional, usually handled by first access)

        // Access configuration
        var config = ConfigurationLoader.Config;

        // Use LoggingService (log level is set by ConfigurationLoader)
        LoggingService.LogInfo("SampleScript Started!");
        LoggingService.LogInfo($"Loaded configuration: Transport='{config.transport}', LogLevel='{config.log_level}'");
        LoggingService.LogDebug("This is a debug message."); // Only shown if log_level is Debug
        LoggingService.LogWarn("This is a warning message.");
        LoggingService.LogError("This is an error message.");

        // Example of changing log level at runtime (not typical use)
        // LoggingService.SetLogLevel(LogLevel.Debug);
        // LoggingService.LogDebug("Debug message after changing level.");
    }
} 