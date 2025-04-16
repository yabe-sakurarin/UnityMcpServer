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
        private const int TestPort = 10000; // Must match connector's BasePort for instanceId "0"
        private const string TestInstanceId = "0";
        private List<string> _receivedMessages = new List<string>();
        private ManualResetEventSlim _messageReceivedEvent;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _messageReceivedEvent = new ManualResetEventSlim(false);

            // Start the test server before each test
            _testServer = new SimpleTcpEchoServer(TestPort);
            var serverStarted = Task.Run(() => _testServer.Start());

            _connector = new UnityInstanceConnector();

            // Clear message list (check for safety)
            if (_receivedMessages == null) _receivedMessages = new List<string>();
            _receivedMessages.Clear();

            // Ensure event is initialized and reset it for the new test
            if (_messageReceivedEvent == null) // Should not happen after init above, but safety check
            {
                Debug.LogError("_messageReceivedEvent is NULL before Reset in SetUp!");
                _messageReceivedEvent = new ManualResetEventSlim(false); // Re-initialize if null
            }
            _messageReceivedEvent.Reset(); // Reset added back AFTER null check

            // Wait briefly for server to potentially start (adjust if needed)
            yield return new WaitUntil(() => serverStarted.IsCompleted || serverStarted.IsFaulted);
            if(serverStarted.IsFaulted)
            {
                Assert.Fail($"Test server failed to start: {serverStarted.Exception?.InnerException?.Message}");
            }
            Debug.Log("Test Server Started");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Stop the server and dispose connector after each test
            _connector?.Dispose();
            _testServer?.Stop();
            _connector = null;
            _testServer = null;
            if (_messageReceivedEvent != null)
            {
                 _messageReceivedEvent.Dispose();
                _messageReceivedEvent = null;
            }
            Debug.Log("Test Server Stopped, Connector Disposed");
            yield return null;
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
                         Debug.Log($"[TestServer] Echoed: {line}");
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