using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Treblle.Net.Masking;

namespace Treblle.Net
{
    public class TrebllePayloadSender
    {
        private static readonly string[] TreblleEndpoints = new[]
        {
            "https://rocknrolla.treblle.com",
            "https://punisher.treblle.com",
            "https://sicario.treblle.com"
        };

        private static readonly Random Random = new Random();
        private static readonly HttpClient HttpClient;

        static TrebllePayloadSender()
        {
            // Configure ServicePointManager once at startup for optimal connection pooling
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;
            ServicePointManager.DefaultConnectionLimit = 10; // Allow up to 10 concurrent connections per endpoint
            ServicePointManager.MaxServicePointIdleTime = 90000; // Keep connections alive for 90 seconds
            ServicePointManager.Expect100Continue = false; // Disable Expect: 100-Continue header for better performance
            ServicePointManager.UseNagleAlgorithm = false; // Disable Nagle algorithm for lower latency

            // Create singleton HttpClient with optimized settings
            HttpClient = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                MaxConnectionsPerServer = 10
            })
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        /// <summary>
        /// Selects a random Treblle endpoint for load balancing
        /// </summary>
        private static string GetRandomEndpoint()
        {
            lock (Random)
            {
                var index = Random.Next(TreblleEndpoints.Length);
                return TreblleEndpoints[index];
            }
        }

        public async Task PrepareAndSendJsonAsync(
            TrebllePayload payload,
            Data data,
            Request request,
            Response response,
            Language language,
            Server server,
            Os os,
            string additionalFieldsFromSettings,
            string SdkToken)
        {

            server.Os = os;

            data.Language = language;
            data.Request = request;
            data.Response = response;
            data.Server = server;

            payload.Data = data;

            // Serialize with settings to handle circular references and prevent infinite loops
            var jsonSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MaxDepth = 50, // Prevent infinite recursion
                NullValueHandling = NullValueHandling.Ignore
            };
            var json = JsonConvert.SerializeObject(payload, jsonSettings);

            // Check if masking is disabled
            var disableMasking = System.Configuration.ConfigurationManager.AppSettings["Treblle:DisableMasking"];
            var isMaskingDisabled = !string.IsNullOrWhiteSpace(disableMasking) &&
                                   (disableMasking.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                                    disableMasking.Equals("1", StringComparison.OrdinalIgnoreCase));

            string finalJson;

            if (isMaskingDisabled)
            {
                // Masking is disabled - send raw payload
                Helpers.DebugLogger.LogWarning("Data masking is DISABLED - sending unmasked payload");
                finalJson = json;
            }
            else
            {
                // Masking is enabled (default behavior)
                try
                {
                    // Merge custom fields with default masking map
                    var maskingMap = new Dictionary<string, string>(Constants.MaskingMap);

                    // Read the comma-separated key-value pairs from appSettings
                    if (!string.IsNullOrEmpty(additionalFieldsFromSettings))
                    {
                        // Split the string by commas to get individual key-value pairs
                        var pairs = additionalFieldsFromSettings.Split(',');

                        foreach (var pair in pairs)
                        {
                            var parts = pair.Split(new[] { ": " }, StringSplitOptions.None);

                            if (parts.Length == 2)
                            {
                                maskingMap[parts[0].Trim()] = parts[1].Trim();
                            }
                        }
                    }

                    // Apply masking - this handles RawJsonString objects properly
                    finalJson = json.MaskPayload(maskingMap, "*****");
                    Helpers.DebugLogger.LogInfo("Data masking applied to sensitive fields");
                }
                catch (Exception ex)
                {
                    // If masking fails, send unmasked data rather than losing the event
                    Helpers.DebugLogger.LogError("Masking failed - sending unmasked payload", ex);
                    finalJson = json;
                }
            }

            // Log payload size
            var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(finalJson);
            Helpers.DebugLogger.LogPayloadSize(payloadBytes, "Outgoing");

            // Select a random endpoint for load balancing
            var endpoint = GetRandomEndpoint();
            Helpers.DebugLogger.LogPayloadSent(endpoint);

            // Send payload asynchronously using HttpClient - fully async, no blocking
            await SendPayloadAsync(endpoint, finalJson, SdkToken).ConfigureAwait(false);
        }

        // Legacy synchronous method for backward compatibility
        public void PrepareAndSendJson(
            TrebllePayload payload,
            Data data,
            Request request,
            Response response,
            Language language,
            Server server,
            Os os,
            string additionalFieldsFromSettings,
            string SdkToken)
        {
            // Use sync-over-async for backward compatibility
            PrepareAndSendJsonAsync(payload, data, request, response, language, server, os, additionalFieldsFromSettings, SdkToken)
                .GetAwaiter().GetResult();
        }

        private static async Task SendPayloadAsync(string endpoint, string jsonPayload, string sdkToken)
        {
            try
            {
                using (var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
                    {
                        request.Headers.Add("x-api-key", sdkToken);
                        request.Content = content;

                        using (var response = await HttpClient.SendAsync(request).ConfigureAwait(false))
                        {
                            Helpers.DebugLogger.LogPayloadSentSuccess((int)response.StatusCode);

                            // Read response for debugging if not successful
                            if (!response.IsSuccessStatusCode)
                            {
                                var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                                Helpers.DebugLogger.LogError($"Treblle API error (HTTP {(int)response.StatusCode})",
                                    new Exception(responseBody));
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Network-specific errors (DNS, connection refused, timeout, etc.)
                Helpers.DebugLogger.LogError("Network error sending to Treblle", ex);
            }
            catch (TaskCanceledException ex)
            {
                // Timeout or cancellation
                Helpers.DebugLogger.LogError("Timeout sending to Treblle", ex);
            }
            catch (Exception ex)
            {
                // Catch all other exceptions - never crash host API
                Helpers.DebugLogger.LogError("Unexpected error sending to Treblle", ex);
            }
        }
    }
}
