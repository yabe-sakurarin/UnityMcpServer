# Basic Usage Sample

This sample demonstrates the basic usage of the Unity MCP Server core components:
*   `LoggingService` for logging messages.
*   `ConfigurationLoader` for loading server settings.

## How to Use

1.  Import this sample into your Unity project via the Package Manager.
2.  Create a new scene or open an existing one.
3.  Create an empty GameObject in the scene.
4.  Attach the `SampleScript.cs` component (found in the `Samples/Unity MCP Server/BasicUsage` folder) to the GameObject.
5.  (Optional) Create a `mcp_config.json` file in your project's root directory (outside the `Assets` folder) to customize settings. Example:
    ```json
    {
      "transport": "websocket",
      "websocket_port": 8888,
      "log_level": "Debug"
    }
    ```
6.  Run the scene.
7.  Check the Unity Console window. You should see log messages prefixed with `[UnityMcpServer]`. The output will depend on the `log_level` setting (either from the config file or the default "Info"). 