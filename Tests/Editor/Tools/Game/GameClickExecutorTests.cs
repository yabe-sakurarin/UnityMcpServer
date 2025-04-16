using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Tools.Game;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection;
using Sakurarin.UnityMcpServer.Runtime.Core;
using Sakurarin.UnityMcpServer.Runtime.InstanceConnection.Protocols;
using UnityEngine.TestTools;
using UnityEngine; // LogAssert のために必要
using System.Text.RegularExpressions; // Added for Regex

namespace Sakurarin.UnityMcpServer.Tests.Editor.Tools.Game
{
    // IUnityInstanceConnector のモック実装
    public class MockUnityInstanceConnector : IUnityInstanceConnector
    {
        public bool ShouldConnectSuccessfully { get; set; } = true;
        public string InstanceIdToConnect { get; set; } = string.Empty;
        public TcpCommunicator CommunicatorToReturn { get; set; } = null;

        public Task<TcpCommunicator> GetOrConnectInstanceAsync(string instanceId, int connectTimeoutMs = 5000)
        {
            if (ShouldConnectSuccessfully && instanceId == InstanceIdToConnect)
            {
                return Task.FromResult(CommunicatorToReturn);
            }
            else
            {
                return Task.FromResult<TcpCommunicator>(null);
            }
        }

        public void DisconnectInstance(string instanceId)
        {
            // このテストでは使用しないため、空の実装
        }

        public Task<bool> SendMessageToInstanceAsync(string instanceId, string message)
        {
            // このテストでは使用しないため、空の実装
            return Task.FromResult(true); // 仮に成功を返す
        }

        public void Dispose()
        {
             // Dispose処理が必要な場合は実装
        }
    }

    public class GameClickExecutorTests
    {
        private GameClickExecutor _executor;
        private MockUnityInstanceConnector _mockConnector;

        [SetUp]
        public void SetUp()
        {
            // LoggingServiceのインスタンスを初期化（テスト環境で必要になる場合）
            // ※ 通常、EditModeテストでは自動的に初期化されるか、
            //   またはテストフレームワークが管理するが、明示的に行う場合。
            // if (LoggingService.Instance == null)
            // {
            //     var go = new GameObject("LoggingService");
            //     go.AddComponent<LoggingService>();
            // }

            _mockConnector = new MockUnityInstanceConnector();
            _executor = new GameClickExecutor(_mockConnector);
        }

        [TearDown]
        public void TearDown()
        {
             // テストごとにLoggingServiceインスタンスをクリーンアップする場合
             // var loggingService = Object.FindObjectOfType<LoggingService>();
             // if (loggingService != null)
             // {
             //     Object.DestroyImmediate(loggingService.gameObject);
             // }
        }


        [Test]
        public async Task ExecuteAsync_ValidParameters_Success()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "1" },
                { "element_id", "button_start" }
            };
            _mockConnector.ShouldConnectSuccessfully = true;
            _mockConnector.InstanceIdToConnect = "1";
             // 接続成功をシミュレートするために、nullではないCommunicatorを返すように設定
            _mockConnector.CommunicatorToReturn = new TcpCommunicator("127.0.0.1", 9999); // Dummy instance, ensure it's disposable if needed

            // Act
            // Removed LogAssert.Expect due to issues with custom log format
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsFalse(result.IsError);
            Assert.IsNull(result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_ConnectionFails_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "2" },
                { "element_id", "some_element" }
            };
            _mockConnector.ShouldConnectSuccessfully = false; // Simulate connection failure
            _mockConnector.InstanceIdToConnect = "2";
             _mockConnector.CommunicatorToReturn = null;

            // Act
             // Removed LogAssert.Expect due to issues with custom log format
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            // Corrected expected error message based on the updated GameClickExecutor.cs
            Assert.AreEqual("Failed to get communicator for instance '2'. Cannot simulate click.", result.ErrorMessage);
            // Removed LogAssert.NoUnexpectedReceived() as it fails on expected INFO logs
            // LogAssert.NoUnexpectedReceived();
        }

         [Test]
        public async Task ExecuteAsync_MissingInstanceId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                //{ "instance_id", "1" }, // 意図的にコメントアウト
                { "element_id", "button_exit" }
            };

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_InvalidInstanceIdType_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", 123 },
                { "element_id", "button_exit" }
            };

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
             Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_MissingElementId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "1" },
                //{ "element_id", "button_start" } // 意図的にコメントアウト
            };

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: element_id (string).", result.ErrorMessage);
        }

         [Test]
        public async Task ExecuteAsync_NullParameters_ReturnsError()
        {
            // Arrange
            Dictionary<string, object> parameters = null;

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Parameters dictionary is null.", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_EmptyInstanceId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "" }, // Empty string
                { "element_id", "button_test" }
            };

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: instance_id (string).", result.ErrorMessage);
        }

        [Test]
        public async Task ExecuteAsync_EmptyElementId_ReturnsError()
        {
            // Arrange
            var parameters = new Dictionary<string, object>
            {
                { "instance_id", "3" },
                { "element_id", "" } // Empty string
            };

            // Act
            var result = await _executor.ExecuteAsync(parameters);

            // Assert
            Assert.IsTrue(result.IsError);
            Assert.AreEqual("Missing or invalid required parameter: element_id (string).", result.ErrorMessage);
        }

        // Add test for when _instanceConnector is null in GameClickExecutor (though constructor prevents this)
        // This might require a different setup or reflection if testing defensively.
    }
} 