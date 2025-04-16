using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;

namespace Sakurarin.UnityMcpServer.Runtime.InstanceConnection
{
    /// <summary>
    /// Manages connections to multiple Unity game instances.
    /// Acts as a central hub for communicating with specific instances via their ID.
    /// </summary>
    public class UnityInstanceConnector : IDisposable
    {
        // Thread-safe dictionary to store active connections
        private readonly ConcurrentDictionary<string, TcpCommunicator> _activeConnections = new ConcurrentDictionary<string, TcpCommunicator>();

        // TODO: Implement a proper way to resolve instance_id to host/port
        // For now, using a simple placeholder logic (e.g., localhost + base port + instance id as offset?)
        private const string DefaultHost = "127.0.0.1";
        private const int BasePort = 10000;

        /// <summary>
        /// Gets the communicator for a specific instance ID, connecting if necessary.
        /// </summary>
        /// <param name="instanceId">The unique ID of the target Unity instance.</param>
        /// <param name="connectTimeoutMs">Timeout for the connection attempt.</param>
        /// <returns>The TcpCommunicator for the instance, or null if connection failed.</returns>
        public async Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                LoggingService.LogError("Instance ID cannot be null or empty.");
                return null;
            }

            if (_activeConnections.TryGetValue(instanceId, out var existingCommunicator) && existingCommunicator.IsConnected)
            {
                // LoggingService.LogDebug($"Reusing existing connection for instance: {instanceId}");
                return existingCommunicator;
            }

            // Resolve host and port (Placeholder implementation)
            string host = DefaultHost;
            int port = ResolvePortForInstance(instanceId);
            if (port == -1) return null; // Error logged in ResolvePort

            LoggingService.LogInfo($"Attempting connection to instance {instanceId} at {host}:{port}");
            var newCommunicator = new TcpCommunicator(host, port);

            // Subscribe to disconnect event to remove from dictionary
            newCommunicator.Disconnected += () => OnInstanceDisconnected(instanceId);
            // TODO: Handle MessageReceived if needed at this level

            bool connected = await newCommunicator.ConnectAsync(connectTimeoutMs).ConfigureAwait(false);

            if (connected)
            {
                // Add or update the dictionary entry
                _activeConnections.AddOrUpdate(instanceId, newCommunicator, (key, oldComm) =>
                {
                    oldComm?.Dispose(); // Dispose the old one if it exists but wasn't connected
                    return newCommunicator;
                });
                LoggingService.LogInfo($"Successfully established connection for instance: {instanceId}");
                return newCommunicator;
            }
            else
            {
                LoggingService.LogError($"Failed to connect to instance: {instanceId} at {host}:{port}");
                newCommunicator.Dispose(); // Clean up the failed communicator
                 // Ensure it's removed if a previous (disconnected) entry existed
                _activeConnections.TryRemove(instanceId, out _);
                return null;
            }
        }

        /// <summary>
        /// Sends a message to a specific instance.
        /// </summary>
        /// <param name="instanceId">The ID of the target instance.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>True if the message was sent successfully, false otherwise.</returns>
        public async Task<bool> SendMessageToInstanceAsync(string instanceId, string message)
        {            if (string.IsNullOrEmpty(instanceId))
            {
                LoggingService.LogError("Instance ID cannot be null or empty when sending message.");
                return false;
            }

            if (_activeConnections.TryGetValue(instanceId, out var communicator) && communicator.IsConnected)
            {
                return await communicator.SendMessageAsync(message).ConfigureAwait(false);
            }
            else
            {
                LoggingService.LogError($"Cannot send message: No active connection found for instance {instanceId}. Please ensure it's connected.");
                return false;
            }
        }

        /// <summary>
        /// Disconnects a specific instance.
        /// </summary>
        /// <param name="instanceId">The ID of the instance to disconnect.</param>
        public void DisconnectInstance(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                LoggingService.LogWarn("Instance ID cannot be null or empty when disconnecting.");
                return;
            }

            if (_activeConnections.TryRemove(instanceId, out var communicator))
            {
                LoggingService.LogInfo($"Disconnecting instance: {instanceId}");
                communicator.Dispose(); // This will trigger cleanup and the Disconnected event (which calls OnInstanceDisconnected)
            }
            else
            {
                LoggingService.LogWarn($"Attempted to disconnect instance {instanceId}, but no active connection was found.");
            }
        }

        /// <summary>
        /// Placeholder method to resolve instance ID to a port.
        /// Needs a proper implementation (e.g., registry lookup, config file).
        /// </summary>
        private int ResolvePortForInstance(string instanceId)
        {
            // Example: Treat instanceId as a numeric offset from a base port
            if (int.TryParse(instanceId, out int idOffset) && idOffset >= 0)
            {
                return BasePort + idOffset;
            }
            else
            {
                // Example: Use a fixed port for a specific named instance
                // if (instanceId == "MainInstance") return BasePort;

                LoggingService.LogError($"Could not resolve port for instance ID: {instanceId}. Invalid format or unknown ID.");
                return -1; // Indicate error
            }
        }

        /// <summary>
        /// Called when a TcpCommunicator signals disconnection.
        /// </summary>
        private void OnInstanceDisconnected(string instanceId)
        {
            LoggingService.LogInfo($"Instance {instanceId} disconnected. Removing from active connections.");
            // TryRemove might have already been called by DisconnectInstance,
            // but this ensures cleanup if the disconnect originated from the communicator itself (e.g., network error).
            _activeConnections.TryRemove(instanceId, out _); // Don't dispose again here, Dispose was called in HandleDisconnect
        }

        /// <summary>
        /// Disposes the connector and all active connections.
        /// </summary>
        public void Dispose()
        {
            LoggingService.LogInfo("Disposing UnityInstanceConnector and all active connections...");
            foreach (var kvp in _activeConnections)
            {
                kvp.Value?.Dispose();
            }
            _activeConnections.Clear();
            GC.SuppressFinalize(this);
        }

        // Optional Finalizer
        ~UnityInstanceConnector()
        {
             LoggingService.LogWarn("UnityInstanceConnector was not explicitly disposed.");
             Dispose();
        }
    }
} 