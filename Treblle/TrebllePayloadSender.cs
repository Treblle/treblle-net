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

            var maskedJson = json.Mask(Constants.MaskingMap, "*****");

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var httpWebRequest = (HttpWebRequest)WebRequest.Create("https://rocknrolla.treblle.com");
            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("x-api-key", ApiKey);

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(maskedJson);
            }

            var httpResponse = httpWebRequest.GetResponse();
        }
    }
}
