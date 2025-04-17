using System.Collections.Generic;
using System.Threading.Tasks;
using Sakurarin.UnityMcpServer.Runtime.Core;

namespace Sakurarin.UnityMcpServer.Runtime.Tools
{
    /// <summary>
    /// Interface for all MCP Tool Executors.
    /// </summary>
    public interface IToolExecutor
    {
        /// <summary>
        /// Gets the unique name of the tool this executor handles (e.g., "game.click").
        /// </summary>
        string ToolName { get; }

        /// <summary>
        /// Executes the tool's logic asynchronously.
        /// </summary>
        /// <param name="parameters">A dictionary containing the parameters passed from the MCP client.</param>
        /// <returns>A Task representing the asynchronous operation, yielding a ToolResult (from Core namespace).</returns>
        Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    }
} 