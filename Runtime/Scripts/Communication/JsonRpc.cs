using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Note: This requires the Newtonsoft.Json package (com.unity.nuget.newtonsoft-json) 
// to be installed via Unity Package Manager.

namespace Sakurarin.UnityMcpServer.Runtime.Communication
{
    /// <summary>
    /// Base class for JSON-RPC messages.
    /// </summary>
    public abstract class JsonRpcMessage
    {
        [JsonProperty("jsonrpc", Required = Required.Always)]
        public string JsonRpcVersion { get; } = "2.0";
    }

    /// <summary>
    /// Represents a JSON-RPC request or notification.
    /// </summary>
    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonProperty("method", Required = Required.Always)]
        public string Method { get; set; }

        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public object Params { get; set; } // Can be JObject, JArray, or null

        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object Id { get; set; } // Can be string, long, or null (for notifications)
    }

    /// <summary>
    /// Represents a JSON-RPC response.
    /// </summary>
    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public object Result { get; set; }

        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }

        [JsonProperty("id", Required = Required.Always)] // Required even for errors
        public object Id { get; set; } // Should match the request ID (string or long)

        /// <summary>
        /// Creates a successful response.
        /// </summary>
        public static JsonRpcResponse SuccessResponse(object id, object result)
        {
            return new JsonRpcResponse { Id = id, Result = result };
        }

        /// <summary>
        /// Creates an error response.
        /// </summary>
        public static JsonRpcResponse ErrorResponse(object id, JsonRpcErrorCode code, string message, object data = null)
        {
            return new JsonRpcResponse { Id = id, Error = new JsonRpcError(code, message, data) };
        }

        /// <summary>
        /// Creates an error response using standard codes.
        /// </summary>
        public static JsonRpcResponse ErrorResponse(object id, StandardRpcError errorType, string message = null, object data = null)
        {
            var standardError = JsonRpcErrors.GetStandardError(errorType);
            return ErrorResponse(id, (JsonRpcErrorCode)standardError.Code, message ?? standardError.Message, data);
        }
    }

    /// <summary>
    /// Represents a JSON-RPC error object.
    /// </summary>
    public class JsonRpcError
    {
        [JsonProperty("code", Required = Required.Always)]
        public long Code { get; set; }

        [JsonProperty("message", Required = Required.Always)]
        public string Message { get; set; }

        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public object Data { get; set; }

        public JsonRpcError(JsonRpcErrorCode code, string message, object data = null)
        {
            Code = (long)code;
            Message = message;
            Data = data;
        }
    }

    /// <summary>
    /// Standard JSON-RPC 2.0 error codes.
    /// </summary>
    public enum JsonRpcErrorCode : long
    {
        ParseError = -32700,
        InvalidRequest = -32600,
        MethodNotFound = -32601,
        InvalidParams = -32602,
        InternalError = -32603,
        // -32000 to -32099 are reserved for implementation-defined server-errors
        ServerErrorStart = -32000,
        ServerErrorEnd = -32099
    }

    // Helper enums/classes for standard errors
    public enum StandardRpcError
    {
        ParseError,
        InvalidRequest,
        MethodNotFound,
        InvalidParams,
        InternalError
    }

    public static class JsonRpcErrors
    {
        public static JsonRpcError GetStandardError(StandardRpcError errorType)
        {
            switch (errorType)
            {
                case StandardRpcError.ParseError:       return new JsonRpcError(JsonRpcErrorCode.ParseError, "Parse error");
                case StandardRpcError.InvalidRequest:   return new JsonRpcError(JsonRpcErrorCode.InvalidRequest, "Invalid Request");
                case StandardRpcError.MethodNotFound:   return new JsonRpcError(JsonRpcErrorCode.MethodNotFound, "Method not found");
                case StandardRpcError.InvalidParams:    return new JsonRpcError(JsonRpcErrorCode.InvalidParams, "Invalid params");
                case StandardRpcError.InternalError:    return new JsonRpcError(JsonRpcErrorCode.InternalError, "Internal error");
                default:                                return new JsonRpcError(JsonRpcErrorCode.InternalError, "Unknown internal error");
            }
        }
    }
} 