using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core; // For LoggingService
using UnityEngine; // Required for Task extensions like ConfigureAwait

namespace Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols
{
    /// <summary>
    /// Handles TCP communication with a single Unity game instance.
    /// </summary>
    public class TcpCommunicator : IDisposable
    {
        private readonly string _host;
        private readonly int _port;
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;
        private StreamWriter _writer;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public bool IsConnected => _client?.Connected ?? false;

        public event Action<string> MessageReceived;
        public event Action Disconnected;

        public TcpCommunicator(string host, int port)
        {
            _host = host;
            _port = port;
        }

        /// <summary>
        /// Connects to the Unity instance asynchronously.
        /// </summary>
        /// <param name="timeoutMs">Connection timeout in milliseconds.</param>
        /// <returns>True if connection succeeded, false otherwise.</returns>
        public async Task<bool> ConnectAsync(int timeoutMs = 5000)
        {
            if (IsConnected)
            {
                LoggingService.LogWarn($"Already connected to {_host}:{_port}.");
                return true;
            }

            // Overall cancellation for this operation
            using var operationCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);

            try
            {
                _client = new TcpClient();
                LoggingService.LogInfo($"Attempting to connect to {_host}:{_port}...");

                // Create the connection task
                var connectTask = _client.ConnectAsync(_host, _port);

                // Create the timeout task
                var delayTask = Task.Delay(timeoutMs, operationCts.Token); // Use operation's token

                // Wait for either task to complete
                var completedTask = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                // Check if timeout occurred first
                if (completedTask == delayTask)
                {
                    // Cancel the ongoing connect attempt (though it might complete immediately after)
                    operationCts.Cancel();
                    LoggingService.LogError($"Connection attempt to {_host}:{_port} timed out after {timeoutMs}ms.");
                     // Ensure ConnectAsync task is observed to prevent unobserved exceptions
                    _ = connectTask.ContinueWith(t => { if (t.IsFaulted) LoggingService.LogDebug($"Connect task failed after timeout: {t.Exception.InnerException}"); }, TaskContinuationOptions.OnlyOnFaulted);
                    CleanUp();
                    return false;
                }

                // If connectTask completed first, check its status
                if (connectTask.IsCompletedSuccessfully && _client.Connected)
                {
                    _stream = _client.GetStream();
                    var encoding = new UTF8Encoding(false);
                    _reader = new StreamReader(_stream, encoding);
                    _writer = new StreamWriter(_stream, encoding) { AutoFlush = true };

                    LoggingService.LogInfo($"Successfully connected to {_host}:{_port}.");
                    _ = Task.Run(() => ListenLoop(_cts.Token), _cts.Token); // Start listening (use original _cts)
                    return true;
                }
                else // connectTask completed but faulted or client not connected
                {
                    // Log the exception if faulted
                    if (connectTask.IsFaulted)
                    {
                        LoggingService.LogError($"Connection attempt to {_host}:{_port} failed: {connectTask.Exception?.InnerException?.Message}");
                    }
                    else
                    {
                        LoggingService.LogError($"Connection attempt to {_host}:{_port} completed but client is not connected.");
                    }
                    CleanUp();
                    return false;
                }
            }
            catch (OperationCanceledException) when (operationCts.IsCancellationRequested)
            {
                 // This catches cancellation triggered by _cts (external) before timeout
                 LoggingService.LogInfo($"Connection attempt to {_host}:{_port} was cancelled externally.");
                 CleanUp();
                 return false;
            }
            catch (Exception ex) // Catch other unexpected errors during setup
            {
                LoggingService.LogError($"Unexpected error during connection attempt to {_host}:{_port}: {ex.Message}");
                CleanUp();
                return false;
            }
        }

        /// <summary>
        /// Sends a message to the connected Unity instance asynchronously.
        /// Assumes messages are delimited by newlines.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <returns>True if the message was sent successfully, false otherwise.</returns>
        public async Task<bool> SendMessageAsync(string message)
        {
            if (!IsConnected || _writer == null)
            {
                LoggingService.LogError("Cannot send message: Not connected.");
                return false;
            }

            try
            {
                // Ensure message ends with a newline for readline-based receiving
                if (!message.EndsWith("\n"))
                {
                    message += "\n";
                }
                await _writer.WriteAsync(message).ConfigureAwait(false);
                // LoggingService.LogDebug($"Sent to {_host}:{_port}: {message.TrimEnd('\n')}"); // Potentially verbose
                return true;
            }
            catch (ObjectDisposedException)
            {
                 LoggingService.LogWarn("Cannot send message: Connection is closing or closed.");
                 return false;
            }
            catch (IOException ioEx)
            {
                LoggingService.LogError($"IO Error sending message to {_host}:{_port}: {ioEx.Message}");
                HandleDisconnect();
                return false;
            }
            catch (Exception ex)
            {
                LoggingService.LogError($"Error sending message to {_host}:{_port}: {ex.Message}");
                HandleDisconnect(); // Assume connection is compromised on unexpected errors
                return false;
            }
        }

        /// <summary>
        /// Loop to continuously listen for incoming messages.
        /// </summary>
        private async void ListenLoop(CancellationToken cancellationToken)
        {
            LoggingService.LogDebug($"Starting listen loop for {_host}:{_port}");
            try
            {
                // Ensure we are on a background thread
                 await Task.Yield();

                while (IsConnected && !cancellationToken.IsCancellationRequested && _reader != null)
                {
                    try
                    {
                        // Use ReadLineAsync which waits for a newline character
                        string receivedMessage = await _reader.ReadLineAsync().ConfigureAwait(false);

                        if (receivedMessage == null)
                        {
                            // End of stream reached (connection closed by peer)
                            LoggingService.LogInfo($"Connection to {_host}:{_port} closed by remote host.");
                            HandleDisconnect();
                            break; // Exit loop
                        }

                        // LoggingService.LogDebug($"Received from {_host}:{_port}: {receivedMessage}"); // Potentially verbose
                        // Use Task.Run to switch back to Unity main thread if needed for event handlers,
                        // but for now, invoke directly from background thread. Consider thread safety in handlers.
                        MessageReceived?.Invoke(receivedMessage);

                    }
                    catch (ObjectDisposedException)
                    {
                        // Stream or reader was disposed, likely during disconnect
                        LoggingService.LogDebug($"Listen loop for {_host}:{_port} stopped due to object disposal.");
                        break;
                    }
                    catch (IOException ioEx)
                    {
                        // Handle network errors (e.g., connection reset)
                         if (!cancellationToken.IsCancellationRequested) // Avoid logging error if we initiated the disconnect
                         {
                            LoggingService.LogError($"IO Error reading from {_host}:{_port}: {ioEx.Message}. Assuming disconnect.");
                            HandleDisconnect();
                         }
                        break; // Exit loop
                    }
                     catch (Exception ex) when (!(ex is OperationCanceledException)) // Ignore cancellation exceptions
                    {
                         if (!cancellationToken.IsCancellationRequested)
                         {
                            LoggingService.LogError($"Unexpected error in listen loop for {_host}:{_port}: {ex.Message}");
                            HandleDisconnect(); // Assume connection is compromised
                         }
                        break; // Exit loop
                    }
                }
            }
            finally
            {
                LoggingService.LogDebug($"Exiting listen loop for {_host}:{_port}");
                // Ensure cleanup happens even if loop exits unexpectedly
                if (IsConnected) // Check if HandleDisconnect wasn't already called
                {
                     HandleDisconnect(); // Trigger disconnect event if connection was still technically open
                }
            }
        }


        /// <summary>
        /// Handles the disconnection logic and invokes the Disconnected event.
        /// </summary>
        private void HandleDisconnect()
        {
            if (!IsConnected && _client == null) return; // Already handled

            LoggingService.LogInfo($"Disconnecting from {_host}:{_port}...");
            CleanUp();
            // Invoke on main thread if needed? For now, invoke directly.
            Disconnected?.Invoke();
        }


        /// <summary>
        /// Cleans up resources (TcpClient, streams, etc.).
        /// </summary>
        private void CleanUp()
        {
             // Cancel any ongoing operations like connect or listen
            if (!_cts.IsCancellationRequested)
            {
                 _cts.Cancel();
            }

            // Use null-conditional ?. to avoid exceptions if objects are already null
            _reader?.Dispose();
            _writer?.Dispose();
            _stream?.Dispose();
            _client?.Close(); // Close also disposes the client and stream

            _reader = null;
            _writer = null;
            _stream = null;
            _client = null;

            LoggingService.LogDebug($"Cleanup complete for {_host}:{_port}");
        }

        /// <summary>
        /// Disposes the communicator, ensuring cleanup.
        /// </summary>
        public void Dispose()
        {
            LoggingService.LogDebug($"Disposing TcpCommunicator for {_host}:{_port}.");
            HandleDisconnect(); // Ensure disconnect logic is called
            _cts.Dispose();
             GC.SuppressFinalize(this); // Prevent finalizer from running if Dispose is called
        }

         // Optional: Finalizer as a safeguard, though relying on Dispose is better.
        ~TcpCommunicator()
        {
            LoggingService.LogWarn($"TcpCommunicator for {_host}:{_port} was not explicitly disposed. Cleaning up in finalizer.");
            CleanUp(); // Use CleanUp directly, not HandleDisconnect, in finalizer
             _cts.Dispose(); // Dispose CancellationTokenSource here too
        }
    }
} 