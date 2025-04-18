using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sakurarin.UnityMcpServer.Runtime.Tests
{
    public class InstanceConnectionTests
    {
        private UnityInstanceConnector _connector;
        private SimpleTcpEchoServer _testServer;
        private const int TestPort = 12345; // Changed to a less common port
        private const string TestInstanceId = "2345"; // Changed ID to match TestPort (assuming base 10000 + ID logic)
        private List<string> _receivedMessages = new List<string>();
        private ManualResetEventSlim _messageReceivedEvent;

        // Renamed to OneTimeSetUp to run once before all tests in this class
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
             Debug.Log("Executing OneTimeSetUp...");
            _messageReceivedEvent = new ManualResetEventSlim(false);
            _receivedMessages = new List<string>(); // Initialize here

            // Start the test server ONCE
            _testServer = new SimpleTcpEchoServer(TestPort);
            try
            {
                _testServer.Start();
                 // Wait a very short moment to allow server thread to start listening
                 // Note: Ideally, the server Start method should provide a way to wait until it's ready.
                 // Task.Delay(100).Wait(); // Simple wait, adjust if needed, avoid in async if possible
                 Debug.Log("Test Server Started (OneTimeSetUp)");
            }
            catch (Exception ex)
            {
                 // Use Assert.Fail in OneTimeSetUp if server fails to start
                 Assert.Fail($"Test server failed to start in OneTimeSetUp: {ex.Message}");
            }

            _connector = new UnityInstanceConnector();
        }

        // Added UnitySetUp for per-test state reset
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            Debug.Log("Executing UnitySetUp...");
            // Clear message list before each test
            _receivedMessages.Clear();

            // Reset the event before each test
             if (_messageReceivedEvent == null) // Safety check, should be initialized in OneTimeSetUp
            {
                 Debug.LogError("_messageReceivedEvent is NULL in UnitySetUp!");
                 _messageReceivedEvent = new ManualResetEventSlim(false);
            }
            _messageReceivedEvent.Reset();
            yield return null; // Required for IEnumerator UnitySetUp
        }

        // Renamed to OneTimeTearDown to run once after all tests in this class
        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Debug.Log("Executing OneTimeTearDown...");
            // Stop the server and dispose connector ONCE
            _connector?.Dispose();
            _testServer?.Stop();
            _connector = null;
            _testServer = null;
            if (_messageReceivedEvent != null)
            {
                _messageReceivedEvent.Dispose();
                _messageReceivedEvent = null;
            }
            Debug.Log("Test Server Stopped, Connector Disposed (OneTimeTearDown)");
        }

        // Basic connection test
        [UnityTest]
        public IEnumerator CanConnectToInstance()
        {
            Task<TcpCommunicator> connectTask = null;
            yield return TaskToCoroutine(async () =>
            {
                connectTask = _connector.GetOrConnectInstanceAsync(TestInstanceId);
                await connectTask;
            });

            Assert.IsNotNull(connectTask?.Result, "Connector should not be null after connection attempt.");
            Assert.IsTrue(connectTask.Result.IsConnected, "Connector should be connected.");
        }

        // Test sending and receiving (echo)
        [UnityTest]
        public IEnumerator CanSendAndReceiveEchoMessage()
        {
            Debug.Log("Starting CanSendAndReceiveEchoMessage test...");
            TcpCommunicator communicator = null;
            bool messageSent = false;
            string testMessage = "Hello Instance!";

            // 1. Connect
             yield return TaskToCoroutine(async () =>
            {
                Debug.Log("Connecting...");
                communicator = await _connector.GetOrConnectInstanceAsync(TestInstanceId);
                Debug.Log($"Connected: {communicator != null}, IsConnected: {communicator?.IsConnected}");
                Assert.IsNotNull(communicator, "Failed to connect for send/receive test.");
                Assert.IsTrue(communicator.IsConnected, "Communicator not connected.");

                Debug.Log("Subscribing to MessageReceived...");
                communicator.MessageReceived += OnMessageReceived;

                Debug.Log("Sending message...");
                messageSent = await _connector.SendMessageToInstanceAsync(TestInstanceId, testMessage);
                Debug.Log($"Message sent: {messageSent}");
            });

            Assert.IsTrue(messageSent, "Failed to send message.");
            Debug.Log("Waiting for message event...");
            bool eventSet = _messageReceivedEvent.Wait(TimeSpan.FromSeconds(5)); // 5 second timeout
            Debug.Log($"Message event set: {eventSet}");
            Assert.IsTrue(eventSet, "Timed out waiting for echo message.");

            // 5. Verify received message
            Assert.AreEqual(1, _receivedMessages.Count, "Expected exactly one message.");
            Assert.AreEqual(testMessage, _receivedMessages[0], "Received message mismatch.");

             // Cleanup listener
             if (communicator != null)
             {
                communicator.MessageReceived -= OnMessageReceived;
             }
        }

        // Helper to run Task in a coroutine
        private IEnumerator TaskToCoroutine(Func<Task> taskFunc)
        {
            var task = taskFunc();
            while (!task.IsCompleted)
            {
                yield return null;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"Task failed: {task.Exception}");
                Assert.Fail(task.Exception?.InnerException?.Message ?? task.Exception?.Message);
            }
        }

        private void OnMessageReceived(string message)
        {
            Debug.Log($"Test received message: {message}");
            _receivedMessages.Add(message);
            _messageReceivedEvent.Set(); // Signal that a message arrived
        }
    }

    // --- Simple TCP Echo Server for Testing ---
    internal class SimpleTcpEchoServer
    {
        private readonly int _port;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private List<Task> _clientTasks = new List<Task>();

        public SimpleTcpEchoServer(int port)
        {
            _port = port;
        }

        public void Start()
        {
            try
            {
                _cts = new CancellationTokenSource();
                _listener = new TcpListener(IPAddress.Loopback, _port);
                _listener.Start();
                Debug.Log($"[TestServer] Listening on port {_port}...");
                // Start accepting clients in a background task
                 _ = Task.Run(AcceptClientsAsync, _cts.Token);
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[TestServer] Failed to start: {ex.Message}");
                 throw; // Re-throw to fail the test setup
            }
        }

        private async Task AcceptClientsAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                     Debug.Log("[TestServer] Waiting for client...");
                    TcpClient client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                     Debug.Log("[TestServer] Client connected!");
                    // Handle each client in its own task
                    _clientTasks.Add(Task.Run(() => HandleClientAsync(client, _cts.Token), _cts.Token));
                    // Clean up completed tasks occasionally (optional)
                    _clientTasks.RemoveAll(t => t.IsCompleted);
                }
            }
             catch (ObjectDisposedException) { /* Listener stopped, expected */ }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                 Debug.LogError($"[TestServer] Error accepting clients: {ex.Message}");
            }
            finally
            {
                Debug.Log("[TestServer] Accept loop stopped.");
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken token)
        {
             Debug.Log("[TestServer] Handling client...");
            using (client)
            using (var stream = client.GetStream())
            // Use same encoding as communicator
            using (var reader = new StreamReader(stream, new UTF8Encoding(false)))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true })
            {
                try
                {
                    while (!token.IsCancellationRequested && client.Connected)
                    {
                        string line = await reader.ReadLineAsync().ConfigureAwait(false);
                        if (line == null) break; // Client disconnected

                        Debug.Log($"[TestServer] Received: {line}");
                        // Echo back
                        await writer.WriteLineAsync(line).ConfigureAwait(false);
                         Debug.Log($"[TestServer] Echoed: {line}"); // Uncommented the echo log
                    }
                }
                 catch (ObjectDisposedException) { /* Client disconnected, expected */ }
                catch (IOException ioEx)
                {
                    Debug.Log($"[TestServer] IO Error handling client: {ioEx.Message}");
                }
                catch (Exception ex) when (!(ex is OperationCanceledException))
                {
                     Debug.LogError($"[TestServer] Error handling client: {ex.Message}");
                }
                 finally
                 {
                    Debug.Log("[TestServer] Client disconnected.");
                 }
            }
        }

        public void Stop()
        {
            try
            {
                 Debug.Log("[TestServer] Stopping...");
                _cts?.Cancel();
                _listener?.Stop();
                // Wait briefly for client tasks to finish (optional, adjust timeout)
                Task.WhenAll(_clientTasks).Wait(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                 Debug.LogError($"[TestServer] Error stopping: {ex.Message}");
            }
             finally
            {
                 _cts?.Dispose();
                _listener = null;
                _cts = null;
                _clientTasks.Clear();
                 Debug.Log("[TestServer] Stopped.");
            }
        }
    }
} 