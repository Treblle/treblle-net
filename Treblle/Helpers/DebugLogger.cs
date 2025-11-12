using System;
using System.Configuration;
using System.Diagnostics;

namespace Treblle.Net.Helpers
{
    /// <summary>
    /// Comprehensive debug logging system for Treblle SDK
    /// Only outputs when Treblle:Debug is enabled in configuration
    /// </summary>
    internal static class DebugLogger
    {
        private static readonly bool IsDebugEnabled;

        static DebugLogger()
        {
            var debugSetting = ConfigurationManager.AppSettings["Treblle:Debug"];
            IsDebugEnabled = !string.IsNullOrWhiteSpace(debugSetting) &&
                           (debugSetting.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                            debugSetting.Equals("1", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Logs configuration status on SDK initialization
        /// </summary>
        public static void LogConfiguration(string sdkToken, string apiKey)
        {
            if (!IsDebugEnabled) return;

            Log("=== TREBLLE CONFIGURATION ===");

            if (!string.IsNullOrWhiteSpace(sdkToken))
            {
                var maskedToken = MaskValue(sdkToken);
                Log($"✅ SDK Token: {maskedToken}");
            }
            else
            {
                Log("❌ SDK Token: NOT CONFIGURED");
            }

            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                var maskedKey = MaskValue(apiKey);
                Log($"✅ API Key: {maskedKey}");
            }
            else
            {
                Log("❌ API Key: NOT CONFIGURED");
            }

            Log($"🔧 Debug Mode: ENABLED");
            Log($"📦 SDK Version: {Constants.SDK_VERSION}");
            Log("===============================");
        }

        /// <summary>
        /// Logs when the SDK handler/attribute is initialized
        /// </summary>
        public static void LogInitialization(string component)
        {
            if (!IsDebugEnabled) return;
            Log($"🔧 {component} initialized");
        }

        /// <summary>
        /// Logs when a request starts being tracked
        /// </summary>
        public static void LogRequestStarted(string method, string url, string userAgent)
        {
            if (!IsDebugEnabled) return;
            Log($"🚀 Request Started: {method} {url}");
            if (!string.IsNullOrEmpty(userAgent))
            {
                Log($"   User-Agent: {userAgent}");
            }
        }

        /// <summary>
        /// Logs when a request is skipped
        /// </summary>
        public static void LogRequestSkipped(string url, string reason)
        {
            if (!IsDebugEnabled) return;
            Log($"⏭️  Request Skipped: {url}");
            Log($"   Reason: {reason}");
        }

        /// <summary>
        /// Logs when a request completes successfully
        /// </summary>
        public static void LogRequestCompleted(string method, string url, int statusCode, long loadTimeMs)
        {
            if (!IsDebugEnabled) return;

            var emoji = statusCode >= 200 && statusCode < 300 ? "✅" :
                       statusCode >= 400 && statusCode < 500 ? "⚠️" : "❌";

            Log($"{emoji} Request Completed: {method} {url} - Status: {statusCode} - Load Time: {loadTimeMs}ms");
        }

        /// <summary>
        /// Logs data masking information
        /// </summary>
        public static void LogDataMasking(int fieldsMasked)
        {
            if (!IsDebugEnabled) return;
            if (fieldsMasked > 0)
            {
                Log($"🔒 Applied data masking to {fieldsMasked} fields");
            }
        }

        /// <summary>
        /// Logs payload size information
        /// </summary>
        public static void LogPayloadSize(long sizeBytes, string payloadType)
        {
            if (!IsDebugEnabled) return;

            var sizeKb = sizeBytes / 1024.0;
            var sizeMb = sizeBytes / 1048576.0;

            if (sizeMb >= 1)
            {
                Log($"📊 {payloadType} payload size: {sizeMb:F2} MB ({sizeBytes:N0} bytes)");
                if (sizeMb > Constants.MAX_PAYLOAD_MB)
                {
                    Log($"⚠️  Large {payloadType} payload (over {Constants.MAX_PAYLOAD_MB}MB) - content will be replaced");
                }
            }
            else
            {
                Log($"📊 {payloadType} payload size: {sizeKb:F2} KB ({sizeBytes:N0} bytes)");
            }
        }

        /// <summary>
        /// Logs when payload is sent to Treblle
        /// </summary>
        public static void LogPayloadSent(string endpoint)
        {
            if (!IsDebugEnabled) return;
            Log($"📤 Sending payload to Treblle: {endpoint}");
        }

        /// <summary>
        /// Logs successful Treblle API response
        /// </summary>
        public static void LogPayloadSentSuccess(int statusCode)
        {
            if (!IsDebugEnabled) return;
            Log($"✅ Payload sent to Treblle - Response: {statusCode}");
        }

        /// <summary>
        /// Logs when an error occurs
        /// </summary>
        public static void LogError(string context, Exception ex)
        {
            if (!IsDebugEnabled) return;

            Log($"❌ Error in {context}: {ex.Message}");
            if (ex.InnerException != null)
            {
                Log($"   Inner Exception: {ex.InnerException.Message}");
            }
        }

        /// <summary>
        /// Logs a general informational message
        /// </summary>
        public static void LogInfo(string message)
        {
            if (!IsDebugEnabled) return;
            Log($"ℹ️  {message}");
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        public static void LogWarning(string message)
        {
            if (!IsDebugEnabled) return;
            Log($"⚠️  {message}");
        }

        /// <summary>
        /// Core logging method - outputs to both Console and Debug
        /// </summary>
        private static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var formattedMessage = $"[{timestamp}] [TREBLLE DEBUG] {message}";

            // Output to both Console and Debug trace
            Console.WriteLine(formattedMessage);
            Debug.WriteLine(formattedMessage);
        }

        /// <summary>
        /// Masks sensitive values for logging (shows first 4 and last 4 characters)
        /// </summary>
        private static string MaskValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "****";
            if (value.Length <= 8) return "****";

            var start = value.Substring(0, 4);
            var end = value.Substring(value.Length - 4);
            return $"{start}****{end}";
        }
    }
}
