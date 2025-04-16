using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;

namespace Sakurarin.UnityMcpServer.Runtime.InstanceConnection
{
    /// <summary>
    /// Interface for managing connections to Unity game instances.
    /// </summary>
    public interface IUnityInstanceConnector
    {
        /// <summary>
        /// Gets the communicator for a specific instance ID, connecting if necessary.
        /// </summary>
        Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000);

        /// <summary>
        /// Sends a message to a specific instance.
        /// </summary>
        Task<bool> SendMessageToInstanceAsync(string instanceId, string message);

        /// <summary>
        /// Disconnects a specific instance.
        /// </summary>
        void DisconnectInstance(string instanceId);

        // Add other necessary methods from UnityInstanceConnector if needed for testing
    }
} 