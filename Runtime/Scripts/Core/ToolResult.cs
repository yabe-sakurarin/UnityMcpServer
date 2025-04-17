using System.Collections.Generic;

namespace Sakurarin.UnityMcpServer.Runtime.Core
{
    /// <summary>
    /// Represents the result of executing an MCP tool.
    /// </summary>
    public class ToolResult
    {
        /// <summary>
        /// Gets a value indicating whether the tool execution resulted in an error.
        /// </summary>
        public bool IsError { get; }

        /// <summary>
        /// Gets the error message if the execution resulted in an error; otherwise, null.
        /// </summary>
        public string ErrorMessage { get; }

        /// <summary>
        /// Gets the data returned by the tool upon successful execution, if any.
        /// The structure of this dictionary depends on the specific tool.
        /// </summary>
        public Dictionary<string, object> ResultData { get; }

        // Protected constructor to enforce usage of factory methods.
        protected ToolResult(bool isError, string errorMessage, Dictionary<string, object> resultData = null)
        {
            IsError = isError;
            ErrorMessage = errorMessage;
            // Ensure ResultData is at least an empty dictionary if not provided and not an error
            // For errors, it can be null.
            ResultData = resultData ?? (!isError ? new Dictionary<string, object>() : null);
        }

        /// <summary>
        /// Creates a ToolResult representing an error.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <returns>A new ToolResult instance indicating an error.</returns>
        public static ToolResult Error(string message)
        {
            return new ToolResult(true, message, null);
        }

        /// <summary>
        /// Creates a ToolResult representing a successful execution with no specific data to return.
        /// </summary>
        /// <returns>A new ToolResult instance indicating simple success.</returns>
        public static ToolResult SimpleSuccess()
        {
            return new ToolResult(false, null, null);
        }

        /// <summary>
        /// Creates a ToolResult representing a successful execution with data to return.
        /// </summary>
        /// <param name="data">The dictionary containing the result data.</param>
        /// <returns>A new ToolResult instance indicating success with data.</returns>
        public static ToolResult SuccessWithData(Dictionary<string, object> data)
        {
            return new ToolResult(false, null, data);
        }
    }
} 