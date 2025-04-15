using System;
using System.IO;
using UnityEngine;

namespace Sakurarin.UnityMcpServer.Runtime.Core
{
    [Serializable] // For JsonUtility
    public class McpServerConfig
    {
        // Default values
        public string transport = "stdio";
        public int websocket_port = 9999;
        public string log_level = "Info";
        public string unity_instance_registry_url = null;
        // Add tool_timeouts_ms later if needed

        // Non-serialized property for easy LogLevel access
        public LogLevel GetLogLevel()
        {
            if (Enum.TryParse<LogLevel>(log_level, true, out var level))
            {
                return level;
            }
            LoggingService.LogWarn($"Invalid log_level '{log_level}' in config. Defaulting to Info.");
            return LogLevel.Info; // Default if parsing fails
        }
    }

    public static class ConfigurationLoader
    {
        private static McpServerConfig _config;
        private const string DefaultConfigFileName = "mcp_config.json";

        public static McpServerConfig Config
        {
            get
            {
                if (_config == null)
                {
                    LoadConfig(); // Load default on first access if not loaded
                }
                return _config;
            }
        }

        // Load configuration from a specific path or default location
        public static void LoadConfig(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                // Default path (e.g., next to executable or in Application.persistentDataPath)
                // For simplicity, let's assume it's in the project root during development/editor
                #if UNITY_EDITOR
                filePath = Path.Combine(Application.dataPath, "..", DefaultConfigFileName);
                #else
                // In a build, Application.dataPath points elsewhere. Consider persistentDataPath or StreamingAssets.
                filePath = Path.Combine(Application.persistentDataPath, DefaultConfigFileName);
                #endif
            }

            LoggingService.LogInfo($"Attempting to load configuration from: {filePath}");

            _config = new McpServerConfig(); // Initialize with defaults

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    JsonUtility.FromJsonOverwrite(json, _config);
                    LoggingService.LogInfo("Configuration loaded successfully.");
                }
                else
                {
                    LoggingService.LogWarn($"Configuration file not found at {filePath}. Using default values.");
                    // Optionally, save a default config file here if it doesn't exist
                    // SaveConfig(filePath, _config);
                }
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to load or parse configuration file at {filePath}. Using default values. Error: {ex.Message}");
                _config = new McpServerConfig(); // Reset to defaults on error
            }

            // Apply loaded log level
            LoggingService.SetLogLevel(_config.GetLogLevel());
        }

        // Optional: Method to save the current config (e.g., to create a default file)
        /*
        public static void SaveConfig(string filePath, McpServerConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true); // Pretty print
                File.WriteAllText(filePath, json);
                LoggingService.LogInfo($"Default configuration saved to {filePath}");
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Failed to save configuration file to {filePath}. Error: {ex.Message}");
            }
        }
        */
    }
} 