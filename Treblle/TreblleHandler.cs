using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Treblle.Net.Helpers;

namespace Treblle.Net
{
    /// <summary>
    /// Global HTTP message handler that automatically tracks all Web API requests
    /// </summary>
    public class TreblleHandler : DelegatingHandler
    {
        private string _sdkToken;
        private string _apiKey;
        private string[] _excludedPaths;

        public TreblleHandler()
        {
            _sdkToken = ConfigurationManager.AppSettings["Treblle:SdkToken"];
            _apiKey = ConfigurationManager.AppSettings["Treblle:ApiKey"];

            // Parse excluded paths from configuration
            var excludedPathsConfig = ConfigurationManager.AppSettings["Treblle:ExcludedPaths"];
            _excludedPaths = string.IsNullOrWhiteSpace(excludedPathsConfig)
                ? new string[0]
                : excludedPathsConfig.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(p => p.Trim())
                                     .ToArray();

            // Log initialization
            DebugLogger.LogInitialization("TreblleHandler");
            DebugLogger.LogConfiguration(_sdkToken, _apiKey);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Skip if not configured
            if (string.IsNullOrWhiteSpace(_sdkToken) || string.IsNullOrWhiteSpace(_apiKey))
            {
                DebugLogger.LogRequestSkipped(request.RequestUri.AbsolutePath, "Missing SDK Token or API Key configuration");
                return await base.SendAsync(request, cancellationToken);
            }

            // Check if request should be tracked (filters out static assets, etc.)
            if (!RequestFilter.ShouldTrackRequest(request, _excludedPaths))
            {
                DebugLogger.LogRequestSkipped(request.RequestUri.AbsolutePath, "Filtered out (static asset, excluded path, or non-API content)");
                return await base.SendAsync(request, cancellationToken);
            }

            DebugLogger.LogRequestStarted(request.Method.ToString(), request.RequestUri.AbsoluteUri, request.Headers.UserAgent?.ToString());

            var stopwatch = Stopwatch.StartNew();
            var httpContext = HttpContext.Current;

            // Capture request data
            var payload = new TrebllePayload();
            var data = new Data();
            var treblleRequest = new Request();
            var treblleResponse = new Response();
            var language = new Language();
            var server = new Server();
            var os = new Os();

            try
            {
                // Extract request information
                payload = HttpContextHelper.ExtractTrebllePayloadData(_sdkToken, _apiKey);
                language = EnvironmentHelper.ExtractLanguageData();
                server = HttpContextHelper.ExtractServerData(httpContext.Request);
                os = EnvironmentHelper.ExtractOsData();
                treblleRequest = await ExtractRequestDataAsync(httpContext, request);
            }
            catch (Exception ex)
            {
                // Don't fail the actual request if Treblle has issues
                DebugLogger.LogError("request capture", ex);
            }

            // Execute the actual request
            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Capture error information
                var error = HttpContextHelper.ExtractErrorData(ex);
                data.Errors.Add(error);

                treblleResponse.Code = 500;
                treblleResponse.Size = 0;
                treblleResponse.LoadTime = stopwatch.ElapsedMilliseconds;

                // Send to Treblle even on error
                await SendToTreblleAsync(payload, data, treblleRequest, treblleResponse, language, server, os);

                throw; // Re-throw the exception
            }

            stopwatch.Stop();

            // Check if response should be tracked (filters out HTML, static content, etc.)
            if (!RequestFilter.ShouldTrackResponse(response))
            {
                DebugLogger.LogRequestSkipped(request.RequestUri.AbsolutePath, "Response content type not tracked (HTML, CSS, images, etc.)");
                return response;
            }

            DebugLogger.LogRequestCompleted(request.Method.ToString(), request.RequestUri.AbsoluteUri, (int)response.StatusCode, stopwatch.ElapsedMilliseconds);

            // Capture response data
            try
            {
                await ExtractResponseDataAsync(response, treblleResponse, stopwatch.ElapsedMilliseconds);

                // Add response headers
                if (httpContext?.Response?.Headers != null)
                {
                    treblleResponse.Headers = httpContext.Response.Headers.AllKeys
                        .Where(k => k != null)
                        .ToDictionary(k => k, k => httpContext.Response.Headers[k]);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("response capture", ex);
            }

            // Send to Treblle asynchronously (don't wait for it)
            _ = Task.Run(() => SendToTreblleAsync(payload, data, treblleRequest, treblleResponse, language, server, os));

            return response;
        }

        private async Task<Request> ExtractRequestDataAsync(HttpContext httpContext, HttpRequestMessage request)
        {
            var treblleRequest = new Request
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Ip = httpContext.Request.ServerVariables["REMOTE_ADDR"] ?? "bogon",
                Url = request.RequestUri.AbsoluteUri,
                UserAgent = request.Headers.UserAgent?.ToString() ?? "",
                Method = request.Method.ToString().ToUpper()
            };

            // Extract route path (will be updated if we can get route data)
            var pathAndQuery = request.RequestUri.PathAndQuery.Split(new[] { '?' }, 2);
            treblleRequest.RoutePath = pathAndQuery[0];

            // Parse query string
            if (pathAndQuery.Length > 1 && !string.IsNullOrEmpty(pathAndQuery[1]))
            {
                var queryParams = HttpUtility.ParseQueryString(pathAndQuery[1]);
                treblleRequest.Query = queryParams.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => queryParams[k]);
            }

            // Extract headers
            treblleRequest.Headers = request.Headers
                .Where(h => h.Key != null)
                .ToDictionary(h => h.Key, h => string.Join(";", h.Value));

            // Extract body
            if (request.Content != null)
            {
                var contentLength = request.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > 2097152) // 2MB
                {
                    treblleRequest.Body = new
                    {
                        message = "Request payload over 2MB limit",
                        size_bytes = contentLength.Value,
                        size_mb = Math.Round(contentLength.Value / 1048576.0, 2),
                        treblle_info = "Payload content replaced due to size limit"
                    };
                }
                else if (request.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    try
                    {
                        var bodyJson = await request.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(bodyJson))
                        {
                            treblleRequest.Body = JsonConvert.DeserializeObject<dynamic>(bodyJson);
                        }
                    }
                    catch
                    {
                        // If we can't deserialize, skip the body
                    }
                }
            }

            return treblleRequest;
        }

        private async Task ExtractResponseDataAsync(HttpResponseMessage response, Response treblleResponse, long loadTimeMs)
        {
            treblleResponse.Code = (int)response.StatusCode;
            treblleResponse.LoadTime = loadTimeMs;

            // Extract response headers
            treblleResponse.Headers = response.Headers
                .Where(h => h.Key != null)
                .ToDictionary(h => h.Key, h => string.Join(";", h.Value));

            // Extract body
            if (response.Content != null)
            {
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > 2097152) // 2MB
                {
                    treblleResponse.Body = new
                    {
                        message = "Response payload over 2MB limit",
                        size_bytes = contentLength.Value,
                        size_mb = Math.Round(contentLength.Value / 1048576.0, 2),
                        treblle_info = "Payload content replaced due to size limit"
                    };
                    treblleResponse.Size = (double)contentLength.Value;
                }
                else if (response.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    try
                    {
                        var bodyJson = await response.Content.ReadAsStringAsync();
                        if (!string.IsNullOrEmpty(bodyJson))
                        {
                            treblleResponse.Body = JsonConvert.DeserializeObject<dynamic>(bodyJson);
                        }
                        treblleResponse.Size = contentLength.HasValue ? (double)contentLength.Value : 0;
                    }
                    catch
                    {
                        // If we can't deserialize, skip the body
                    }
                }
            }
        }

        private async Task SendToTreblleAsync(
            TrebllePayload payload,
            Data data,
            Request request,
            Response response,
            Language language,
            Server server,
            Os os)
        {
            try
            {
                server.Os = os;
                data.Language = language;
                data.Request = request;
                data.Response = response;
                data.Server = server;
                payload.Data = data;

                var sender = new TrebllePayloadSender();
                string additionalFieldsFromSettings = ConfigurationManager.AppSettings["Treblle:AdditionalFieldsToMask"];

                await Task.Run(() => sender.PrepareAndSendJson(
                    payload,
                    data,
                    request,
                    response,
                    language,
                    server,
                    os,
                    additionalFieldsFromSettings,
                    _apiKey));
            }
            catch (Exception ex)
            {
                // Don't fail the request if Treblle has issues
                DebugLogger.LogError("Treblle payload sending", ex);
            }
        }
    }
}
