using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sakurarin.UnityMcpServer.Runtime.Tools
{
    /// <summary>
    /// Represents the result of a tool execution.
    /// Conforms loosely to MCP CallToolResult structure.
    /// </summary>
    public class ToolResult
    {
        /// <summary>
        /// Indicates if the tool execution resulted in an error.
        /// Corresponds to MCP 'isError'.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// The content returned by the tool on success.
        /// This should be serializable to JSON as the 'result' field.
        /// Often a dictionary or a simple object (e.g., { "success": true }).
        /// </summary>
        public Dictionary<string, object> Content { get; set; }

        /// <summary>
        /// A descriptive error message if IsError is true.
        /// This might be included within the Content in a structured error format for MCP.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful ToolResult.
        /// </summary>
        /// <param name="content">Optional content dictionary.</param>
        /// <returns>A successful ToolResult.</returns>
        public static ToolResult Success(Dictionary<string, object> content = null)
        {
            // Default success content if not provided
            if (content == null)
            {
                content = new Dictionary<string, object> { { "success", true } };
            }
            return new ToolResult { IsError = false, Content = content };
        }

         /// <summary>
        /// Creates a simple successful ToolResult with { "success": true } content.
        /// </summary>
        /// <returns>A simple successful ToolResult.</returns>
        public static ToolResult SimpleSuccess()
        {
            return Success(new Dictionary<string, object> { { "success", true } });
        }

        /// <summary>
        /// Creates an error ToolResult.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>An error ToolResult.</returns>
        public static ToolResult Error(string message)
        {
            // Include error details in Content for potential MCP formatting
            var errorContent = new Dictionary<string, object>
            {
                { "success", false },
                { "error", message }
                // Future: Add structured error details (code, type) here if needed
            };
            return new ToolResult { IsError = true, ErrorMessage = message, Content = errorContent };
        }
    }

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
        /// <returns>A Task representing the asynchronous operation, yielding a ToolResult.</returns>
        Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
    }
} 