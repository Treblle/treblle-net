using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http.Controllers;

namespace Treblle.Net.Helpers
{
    public static class HttpContextHelper
    {
        public static TrebllePayload ExtractTrebllePayloadData(
     string sdkToken,
     string apiKey)
        {
            var payload = new TrebllePayload();

            payload.Sdk = "net";
            payload.Version = Constants.SDK_VERSION;
            payload.SdkToken = sdkToken;
            payload.ApiKey = apiKey;

            return payload;
        }

        public static Request ExtractRequestData(
            HttpContext httpContext,
            HttpActionContext actionContext)
        {
            var request = new Request();


            request.Timestamp = httpContext.Timestamp.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
            request.Ip = httpContext.Request.ServerVariables["REMOTE_ADDR"] ?? "bogon";
            request.Url = actionContext.Request.RequestUri.AbsoluteUri;

            var pathAndQuery = actionContext.Request.RequestUri.PathAndQuery.Split(new[] { '?' }, 2);

            // Extract route template from Web API routing (e.g., "api/users/{id}/favorites")
            var routeTemplate = actionContext.RequestContext?.RouteData?.Route?.RouteTemplate;
            if (!string.IsNullOrEmpty(routeTemplate))
            {
                // Convert Web API route template format to OpenAPI format
                // {id} -> :id, {userId} -> :userId, etc.
                request.RoutePath = System.Text.RegularExpressions.Regex.Replace(
                    routeTemplate,
                    @"\{(\w+)\}",
                    ":$1");
            }
            else
            {
                // Fallback to actual path if route template is not available
                request.RoutePath = pathAndQuery[0];
            }

            // Parse query string into Dictionary
            if (pathAndQuery.Length > 1 && !string.IsNullOrEmpty(pathAndQuery[1]))
            {
                var queryParams = System.Web.HttpUtility.ParseQueryString(pathAndQuery[1]);
                request.Query = queryParams.AllKeys
                    .Where(k => k != null)
                    .ToDictionary(k => k, k => queryParams[k]);
            }

            request.UserAgent = actionContext.Request.Headers.UserAgent?.ToString() ?? "";
            request.Method = actionContext.Request.Method.ToString().ToUpper();

            request.Body = null;

            if (actionContext.Request.Content.Headers.ContentType != null)
            {
                // Check request payload size
                var contentLength = actionContext.Request.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > 2097152) // 2MB
                {
                    request.Body = new
                    {
                        message = "Request payload over 2MB limit",
                        size_bytes = contentLength.Value,
                        size_mb = Math.Round(contentLength.Value / 1048576.0, 2),
                        treblle_info = "Payload content replaced due to size limit"
                    };
                }
                else if (actionContext.Request.Content.Headers.ContentType.ToString().Contains("application/json"))
                {
                    Stream req = httpContext.Request.InputStream;
                    req.Seek(0, SeekOrigin.Begin);
                    var bodyJson = new StreamReader(req).ReadToEnd();

                    if (IsValidJson(bodyJson))
                    {
                        request.Body = JsonConvert.DeserializeObject<dynamic>(bodyJson);
                    }
                    else
                    {
                        DebugLogger.LogWarning("Invalid JSON in request body");
                    }
                }
                else if (HttpContext.Current.Request.Form != null)
                {
                    var dict = HttpContext.Current.Request.Form.AllKeys.ToDictionary(k => k, k => HttpContext.Current.Request.Form[k]);
                    request.Body = dict;
                }
            }

            if (actionContext.Request.Headers != null)
            {
                try
                {
                    request.Headers = actionContext.Request.Headers.ToDictionary(x => x.Key, x => String.Join(";", x.Value));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Invalid JSON in request");
                }
            }

            return request;
        }

        public static Server ExtractServerData(HttpRequest request)
        {
            var server = new Server();

            string serverIpAddress = request.ServerVariables["LOCAL_ADDR"];
            server.Ip = string.IsNullOrEmpty(serverIpAddress) ? "bogon" : serverIpAddress;
            server.Timezone = (!String.IsNullOrEmpty(TimeZone.CurrentTimeZone.StandardName)) ? TimeZone.CurrentTimeZone.StandardName : "UTC";
            server.Software = request.ServerVariables["SERVER_SOFTWARE"];
            server.Protocol = request.ServerVariables["SERVER_PROTOCOL"];

            return server;
        }

        public static Error ExtractErrorData(Exception exception)
        {
            Error error = new Error();

            error.Source = "onException";
            error.Type = exception.GetType().Name;
            error.Message = exception.Message;
            error.File = null;
            error.Line = 0;

            var stackTrace = new StackTrace(exception, true);
            if (stackTrace != null)
            {
                if (stackTrace.FrameCount > 0)
                {
                    var frame = stackTrace.GetFrame(0);
                    if (frame != null)
                    {
                        var line = frame.GetFileLineNumber();
                        if (line != null)
                        {
                            error.Line = line;
                        }
                        var file = frame.GetFileName();
                        if (file != null)
                        {
                            error.File = file;
                        }
                    }

                }
            }

            return error;
        }

        private static bool IsValidJson(string str)
        {
            try
            {
                JsonConvert.DeserializeObject(str);
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }



    }
}
