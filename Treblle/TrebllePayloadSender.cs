using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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

        public void PrepareAndSendJson(
            TrebllePayload payload,
            Data data,
            Request request,
            Response response,
            Language language,
            Server server,
            Os os,
            string additionalFieldsFromSettings,
            string ApiKey)
        {

            server.Os = os;

            data.Language = language;
            data.Request = request;
            data.Response = response;
            data.Server = server;

            payload.Data = data;

            var json = JsonConvert.SerializeObject(payload);

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
                // Read the comma-separated key-value pairs from appSettings
                if (!string.IsNullOrEmpty(additionalFieldsFromSettings))
                {
                    var additionalFieldsToMask = new Dictionary<string, string>();

                    // Split the string by commas to get individual key-value pairs
                    var pairs = additionalFieldsFromSettings.Split(',');

                    foreach (var pair in pairs)
                    {
                        var parts = pair.Split(new[] { ": " }, StringSplitOptions.None);

                        if (parts.Length == 2)
                        {
                            additionalFieldsToMask[parts[0]] = parts[1];
                        }
                    }

                    if (additionalFieldsToMask.Any())
                    {
                        Constants.MaskingMap.Concat(additionalFieldsToMask);
                    }
                }

                finalJson = json.Mask(Constants.MaskingMap, "*****");
                Helpers.DebugLogger.LogInfo("Data masking applied to sensitive fields");
            }

            // Log payload size
            var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(finalJson);
            Helpers.DebugLogger.LogPayloadSize(payloadBytes, "Outgoing");

            // Select a random endpoint for load balancing
            var endpoint = GetRandomEndpoint();
            Helpers.DebugLogger.LogPayloadSent(endpoint);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(endpoint);
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("x-api-key", ApiKey);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(finalJson);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Helpers.DebugLogger.LogPayloadSentSuccess((int)httpResponse.StatusCode);
        }
    }
}
