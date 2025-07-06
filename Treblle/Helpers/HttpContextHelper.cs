using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Http.Controllers;
using System.Xml.Linq;

namespace Treblle.Net.Helpers
{
    public static class HttpContextHelper
    {
        public static TrebllePayload ExtractTrebllePayloadData(
     string projectId,
     string apiKey)
        {
            var payload = new TrebllePayload();

            payload.Sdk = "net-framework";
            payload.Version = EnvironmentHelper.GetTrimmedSdkVersion();
            payload.ProjectId = projectId;
            payload.ApiKey = apiKey;

            return payload;
        }

        public static Request ExtractRequestData(
            HttpContext httpContext,
            HttpActionContext actionContext)
        {
            var request = new Request();


            request.Timestamp = httpContext.Timestamp.ToUniversalTime().ToString("yyyy-M-d H:m:s");
            request.Ip = httpContext.Request.ServerVariables["REMOTE_ADDR"];
            request.Url = actionContext.Request.RequestUri.AbsoluteUri;
            var pathAndQuery = actionContext.Request.RequestUri.PathAndQuery.Split('?');
            request.RoutePath = pathAndQuery[0];
            request.Query = pathAndQuery[1];
            request.UserAgent = actionContext.Request.Headers.UserAgent.ToString();
            request.Method = actionContext.Request.Method.ToString();

            request.Body = null;

            if (actionContext.Request.Content.Headers.ContentType != null)
            {
                if (actionContext.Request.Content.Headers.ContentType.ToString().Contains("application/json"))
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
                        Console.WriteLine("Invalid JSON in request");
                    }
                }
                else if (actionContext.Request.Content.Headers.ContentType.ToString().Contains("text/plain"))
                {
                    Stream req = HttpContext.Current.Request.InputStream;
                    req.Seek(0, SeekOrigin.Begin);
                    var text = new StreamReader(req).ReadToEnd();

                    request.Body = text;
                }
                else if (actionContext.Request.Content.Headers.ContentType.ToString().Contains("application/xml"))
                {
                    Stream req = HttpContext.Current.Request.InputStream;
                    req.Seek(0, SeekOrigin.Begin);
                    var xmlData = new StreamReader(req).ReadToEnd();

                    XDocument doc = XDocument.Parse(xmlData);
                    string jsonText = JsonConvert.SerializeXNode(doc);
                    if (IsValidJson(jsonText))
                    {
                        request.Body = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
                    }
                    else
                    {
                        Console.WriteLine("Invalid JSON in request");
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

        public static Request ExtractRequestData(HttpContext httpContext, IncomingWebRequestContext incomingWebRequestContext)
        {
            var request = new Request();

            // 1. Timestamp
            request.Timestamp = httpContext?.Timestamp.ToUniversalTime().ToString("yyyy-M-d H:m:s");

            // 2. IP Address
            request.Ip = httpContext?.Request.ServerVariables["REMOTE_ADDR"] ?? "unknown";

            // 3. Request Uri & RoutePath & Query
            var uri = incomingWebRequestContext?.UriTemplateMatch?.RequestUri;
            if (uri == null)
            {
                uri = httpContext?.Request.Url;
            }

            if (uri != null)
            {
                request.Url = uri.AbsoluteUri;
                var pathAndQuery = uri.PathAndQuery.Split(new[] { '?' }, 2);
                request.RoutePath = pathAndQuery[0];
                request.Query = pathAndQuery.Length > 1 ? pathAndQuery[1] : "";
            }

            // 4. User-Agent
            request.UserAgent = httpContext?.Request.Headers["User-Agent"];

            // 5. HTTP Method
            request.Method = incomingWebRequestContext?.Method;

            // 6. Body Parsing (JSON, XML, etc.)
            string contentType = httpContext?.Request.ContentType ?? "";

            if (contentType.Contains("application/json"))
            {
                Stream req = httpContext.Request.InputStream;
                req.Seek(0, SeekOrigin.Begin);
                var bodyJson = new StreamReader(req).ReadToEnd();

                if (IsValidJson(bodyJson))
                {
                    request.Body = JsonConvert.DeserializeObject<dynamic>(bodyJson);
                }
            }
            else if (contentType.Contains("text/plain"))
            {
                Stream req = httpContext.Request.InputStream;
                req.Seek(0, SeekOrigin.Begin);
                request.Body = new StreamReader(req).ReadToEnd();
            }
            else if (contentType.Contains("application/xml"))
            {
                Stream req = httpContext.Request.InputStream;
                req.Seek(0, SeekOrigin.Begin);
                var xmlData = new StreamReader(req).ReadToEnd();

                try
                {
                    XDocument doc = XDocument.Parse(xmlData);
                    string jsonText = JsonConvert.SerializeXNode(doc);
                    if (IsValidJson(jsonText))
                    {
                        request.Body = JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Invalid XML content");
                }
            }
            else if (httpContext?.Request.Form != null)
            {
                var dict = httpContext.Request.Form.AllKeys.ToDictionary(k => k, k => httpContext.Request.Form[k]);
                request.Body = dict;
            }

            // 7. Headers
            try
            {
                var headers = httpContext?.Request?.Headers;
                if (headers != null)
                {
                    request.Headers = headers.AllKeys.ToDictionary(k => k, k => headers[k]);
                }
            }
            catch
            {
                Console.WriteLine("Error extracting headers");
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
            server.Signature = null;
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
