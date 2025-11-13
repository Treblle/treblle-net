using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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
                // Extract request information (metadata only, no body yet)
                payload = HttpContextHelper.ExtractTrebllePayloadData(_sdkToken, _apiKey);
                language = EnvironmentHelper.ExtractLanguageData();
                server = HttpContextHelper.ExtractServerData(httpContext.Request);
                os = EnvironmentHelper.ExtractOsData();
                treblleRequest = await ExtractRequestMetadataAsync(httpContext, request);
            }
            catch (Exception ex)
            {
                // Don't fail the actual request if Treblle has issues
                DebugLogger.LogError("request metadata capture", ex);
            }

            // Execute the actual request first - this allows the controller to consume the request body
            HttpResponseMessage response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);

                // Now extract the body from HttpContext after the request has been processed
                // This uses the buffered input stream if available
                try
                {
                    await ExtractRequestBodyAsync(httpContext, treblleRequest);
                }
                catch (Exception ex)
                {
                    DebugLogger.LogError("request body capture", ex);
                }
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

                // Send to Treblle even on error - but don't let Treblle errors mask the real exception
                try
                {
                    await SendToTreblleAsync(payload, data, treblleRequest, treblleResponse, language, server, os);
                }
                catch (Exception treblleEx)
                {
                    // Log but swallow Treblle errors - the original exception is more important
                    DebugLogger.LogError("Treblle error during exception handling", treblleEx);
                }

                throw; // Re-throw the original exception
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

        private Task<Request> ExtractRequestMetadataAsync(HttpContext httpContext, HttpRequestMessage request)
        {
            var treblleRequest = new Request
            {
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Ip = httpContext.Request.ServerVariables["REMOTE_ADDR"] ?? "bogon",
                Url = request.RequestUri.AbsoluteUri,
                UserAgent = request.Headers.UserAgent?.ToString() ?? "",
                Method = request.Method.ToString().ToUpper()
            };

            // Extract route path from route template
            var pathAndQuery = request.RequestUri.PathAndQuery.Split(new[] { '?' }, 2);

            // Try to get the route template from Web API routing
            var routeData = request.GetRouteData();
            var routeTemplate = routeData?.Route?.RouteTemplate;

            if (!string.IsNullOrEmpty(routeTemplate))
            {
                // Convert Web API route template format to OpenAPI format
                // {id} -> :id, {articleId} -> :articleId, etc.
                treblleRequest.RoutePath = System.Text.RegularExpressions.Regex.Replace(
                    routeTemplate,
                    @"\{(\w+)\}",
                    ":$1");
            }
            else
            {
                // Fallback to actual path if route template is not available
                treblleRequest.RoutePath = pathAndQuery[0];
            }

            // Parse query string
            if (pathAndQuery.Length > 1 && !string.IsNullOrEmpty(pathAndQuery[1]))
            {
                var queryParams = HttpUtility.ParseQueryString(pathAndQuery[1]);
                treblleRequest.Query = queryParams.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => queryParams[k]);
            }

            // Extract headers with pre-allocated dictionary
            var headerCount = request.Headers.Count();
            treblleRequest.Headers = new Dictionary<string, string>(headerCount);
            foreach (var header in request.Headers)
            {
                if (header.Key != null)
                {
                    treblleRequest.Headers[header.Key] = string.Join(";", header.Value);
                }
            }

            return Task.FromResult(treblleRequest);
        }

        private async Task ExtractRequestBodyAsync(HttpContext httpContext, Request treblleRequest)
        {
            // Check if we have a request body to read
            if (httpContext?.Request?.InputStream == null || httpContext.Request.ContentLength <= 0)
            {
                return;
            }

            var contentType = httpContext.Request.ContentType;
            var contentLength = httpContext.Request.ContentLength;

            // Check size limit
            if (contentLength > Constants.MAX_PAYLOAD_BYTES)
            {
                treblleRequest.Body = new
                {
                    message = $"Request payload over {Constants.MAX_PAYLOAD_MB}MB limit",
                    size_bytes = contentLength,
                    size_mb = Math.Round(contentLength / 1048576.0, 2),
                    treblle_info = "Payload content replaced due to size limit"
                };
                return;
            }

            // Only capture JSON bodies
            if (contentType != null && contentType.Contains("application/json"))
            {
                try
                {
                    // Try to read from the input stream
                    // Note: This will only work if the stream is seekable or hasn't been consumed
                    var stream = httpContext.Request.InputStream;

                    if (stream.CanSeek)
                    {
                        stream.Position = 0; // Reset position to beginning
                    }

                    using (var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true))
                    {
                        var bodyJson = await reader.ReadToEndAsync();

                        if (!string.IsNullOrEmpty(bodyJson))
                        {
                            // Store as RawJsonString to avoid double deserialization
                            // Will be deserialized only once during masking
                            treblleRequest.Body = new RawJsonString(bodyJson);
                        }

                        // Reset position if seekable so others can read it
                        if (stream.CanSeek)
                        {
                            stream.Position = 0;
                        }
                    }
                }
                catch
                {
                    // If we can't read the body, skip it silently
                    // The controller may have already consumed it
                }
            }
        }

        private async Task ExtractResponseDataAsync(HttpResponseMessage response, Response treblleResponse, long loadTimeMs)
        {
            treblleResponse.Code = (int)response.StatusCode;
            treblleResponse.LoadTime = loadTimeMs;

            // Extract response headers with pre-allocated dictionary
            var responseHeaderCount = response.Headers.Count();
            treblleResponse.Headers = new Dictionary<string, string>(responseHeaderCount);
            foreach (var header in response.Headers)
            {
                if (header.Key != null)
                {
                    treblleResponse.Headers[header.Key] = string.Join(";", header.Value);
                }
            }

            // Extract body
            if (response.Content != null)
            {
                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > Constants.MAX_PAYLOAD_BYTES)
                {
                    treblleResponse.Body = new
                    {
                        message = $"Response payload over {Constants.MAX_PAYLOAD_MB}MB limit",
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
                            // Store as RawJsonString to avoid double deserialization
                            // Will be deserialized only once during masking
                            treblleResponse.Body = new RawJsonString(bodyJson);
                        }

                        // Calculate size: use ContentLength if available, otherwise calculate from actual body
                        // This handles chunked transfer encoding where ContentLength is null
                        if (contentLength.HasValue)
                        {
                            treblleResponse.Size = (double)contentLength.Value;
                        }
                        else if (!string.IsNullOrEmpty(bodyJson))
                        {
                            treblleResponse.Size = Encoding.UTF8.GetByteCount(bodyJson);
                        }
                        else
                        {
                            treblleResponse.Size = 0;
                        }
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

                // Use fully async method - no Task.Run wrapper needed, no thread pool overhead
                await sender.PrepareAndSendJsonAsync(
                    payload,
                    data,
                    request,
                    response,
                    language,
                    server,
                    os,
                    additionalFieldsFromSettings,
                    _sdkToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Don't fail the request if Treblle has issues
                DebugLogger.LogError("Treblle payload sending", ex);
            }
        }
    }
}
