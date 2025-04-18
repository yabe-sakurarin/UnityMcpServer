using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core; // For LoggingService
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols; // Required for TcpCommunicator return type (even if dummy)

namespace Sakurarin.UnityMcpServer.Runtime.Samples
{
    /// <summary>
    /// Mock implementation of IUnityInstanceConnector for testing purposes.
    /// Does not perform real connections.
    /// </summary>
    public class MockUnityInstanceConnector : IUnityInstanceConnector
    {
        public bool SimulateConnectionSuccess { get; set; } = true; // Control mock behavior
        public bool SimulateSendSuccess { get; set; } = true; // Control mock behavior

        public Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000)
        {
            LoggingService.LogInfo($"[Mock] Attempting to get/connect to instance: {instanceId} (Timeout: {connectTimeoutMs}ms)");

            if (SimulateConnectionSuccess)
            {
                LoggingService.LogInfo($"[Mock] Simulating successful connection for instance: {instanceId}");
                // Corrected: Return a dummy TcpCommunicator instance for success simulation
                // Assuming TcpCommunicator has a constructor like TcpCommunicator(string host, int port)
                try
                {
                    // Use placeholder values for the dummy instance.
                    // This instance won't actually connect to anything.
                    var dummyCommunicator = new TcpCommunicator("mock-host", 0);
                    // Optionally, set IsConnected to true if the executor might check it later
                    // (Requires IsConnected to have a public setter or a way to set it in the mock,
                    //  which might not be the case. For now, assume null check is sufficient.)
                    return Task.FromResult(dummyCommunicator);
                }
                catch(System.Exception ex)
                {
                    // Log error if dummy creation fails (e.g., constructor not found or inaccessible)
                    LoggingService.LogError($"[Mock] Failed to create dummy TcpCommunicator: {ex.Message}. Returning null instead.");
                    return Task.FromResult<TcpCommunicator>(null);
                }
            }
            else
            {
                LoggingService.LogInfo($"[Mock] Simulating failed connection for instance: {instanceId}");
                return Task.FromResult<TcpCommunicator>(null);
            }
        }

        public Task<bool> SendMessageToInstanceAsync(string instanceId, string message)
        {
            LoggingService.LogInfo($"[Mock] Attempting to send message to instance {instanceId}: {message}");
            LoggingService.LogInfo($"[Mock] Simulating send {(SimulateSendSuccess ? "success" : "failure")}");
            return Task.FromResult(SimulateSendSuccess);
        }

        public void DisconnectInstance(string instanceId)
        {
            LoggingService.LogInfo($"[Mock] Disconnecting instance: {instanceId}");
            // No actual connection to disconnect
        }

        public void Dispose()
        {
             LoggingService.LogInfo("[Mock] Dispose called.");
             // Nothing to dispose
        }
    }
} 