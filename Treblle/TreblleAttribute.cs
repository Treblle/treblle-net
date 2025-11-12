using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Treblle.Net.Helpers;

namespace Treblle.Net
{
    public class TreblleAttribute : ActionFilterAttribute
    {

        public string ApiKey = "";
        public string SdkToken = "";

        Stopwatch stopwatch = new Stopwatch();

        TrebllePayload payload = new TrebllePayload();
        Data data = new Data();
        Response response = new Response();
        Request request = new Request();
        Language language = new Language();
        Server server = new Server();
        Os os = new Os();

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                SdkToken = ConfigurationManager.AppSettings["Treblle:SdkToken"];
                ApiKey = ConfigurationManager.AppSettings["Treblle:ApiKey"];

                if (!string.IsNullOrWhiteSpace(SdkToken) && !string.IsNullOrWhiteSpace(ApiKey))
                {
                    stopwatch.Start();

                    payload = HttpContextHelper.ExtractTrebllePayloadData(SdkToken, ApiKey);
                    language = EnvironmentHelper.ExtractLanguageData();
                    server = HttpContextHelper.ExtractServerData(HttpContext.Current.Request);
                    os = EnvironmentHelper.ExtractOsData();
                    request = HttpContextHelper.ExtractRequestData(HttpContext.Current, actionContext);                  
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("OnActionExecuting", ex);
            }

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                SdkToken = ConfigurationManager.AppSettings["Treblle:SdkToken"];
                ApiKey = ConfigurationManager.AppSettings["Treblle:ApiKey"];

                if (!string.IsNullOrWhiteSpace(SdkToken) && !string.IsNullOrWhiteSpace(ApiKey))
                {
                    if (actionExecutedContext.Exception != null)
                    {
                        var error = HttpContextHelper.ExtractErrorData(actionExecutedContext.Exception);
                        data.Errors.Add(error);

                        response.Code = 500;
                        response.Size = 0;

                        response.Body = null;
                    }
                    else
                    {
                        response.Code = (int)actionExecutedContext.Response.StatusCode;
                        if (actionExecutedContext.Response.Content != null)
                        {
                            if (actionExecutedContext.Response.Content.Headers.ContentType.ToString().Contains("application/json"))
                            {
                                var contentLength = actionExecutedContext.Response.Content.Headers.ContentLength;
                                if (contentLength.HasValue && contentLength.Value > Constants.MAX_PAYLOAD_BYTES)
                                {
                                    // Replace response body with descriptive object instead of adding error
                                    response.Body = new
                                    {
                                        message = $"Response payload over {Constants.MAX_PAYLOAD_MB}MB limit",
                                        size_bytes = contentLength.Value,
                                        size_mb = Math.Round(contentLength.Value / 1048576.0, 2),
                                        treblle_info = "Payload content replaced due to size limit"
                                    };
                                    response.Size = (double)contentLength.Value;
                                }
                                else
                                {
                                    // Use GetAwaiter().GetResult() instead of .Result to avoid capturing sync context
                                    // This prevents deadlocks in ASP.NET hosting environments
                                    var outputStream = actionExecutedContext.Response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();

                                    // Ensure stream is seekable before attempting to seek
                                    if (outputStream.CanSeek)
                                    {
                                        outputStream.Seek(0, SeekOrigin.Begin);
                                    }

                                    using (var reader = new StreamReader(outputStream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true))
                                    {
                                        var outputBody = reader.ReadToEnd();

                                        if (IsValidJson(outputBody))
                                        {
                                            response.Body = JsonConvert.DeserializeObject<dynamic>(outputBody);
                                        }
                                        else
                                        {
                                            DebugLogger.LogWarning("Invalid JSON in response body");
                                        }

                                        // Calculate size: use ContentLength if available, otherwise calculate from actual body
                                        // This handles chunked transfer encoding where ContentLength is null
                                        if (contentLength.HasValue)
                                        {
                                            response.Size = (double)contentLength.Value;
                                        }
                                        else if (!string.IsNullOrEmpty(outputBody))
                                        {
                                            response.Size = System.Text.Encoding.UTF8.GetByteCount(outputBody);
                                        }
                                        else
                                        {
                                            response.Size = 0;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    stopwatch.Stop();
                    response.LoadTime = stopwatch.ElapsedMilliseconds;

                    Action<HttpContext> AddOnRequestCompletedCallback = httpContext =>
                    {
                        if (httpContext.Response.Headers != null)
                        {

                            try
                            {
                                Dictionary<string, string> headersDict = new Dictionary<string, string>();
                                foreach (var key in httpContext.Response.Headers.AllKeys)
                                {
                                    var value = httpContext.Response.Headers.Get(key);
                                    headersDict.Add(key, value);
                                }
                                response.Headers = headersDict;
                            }
                            catch (Exception ex)
                            {
                                DebugLogger.LogError("extracting response headers", ex);
                            }
                        }

                        string additionalFieldsFromSettings = ConfigurationManager.AppSettings["Treblle:AdditionalFieldsToMask"];
                        var treblleSender = new TrebllePayloadSender();

                        treblleSender.PrepareAndSendJson(
                            payload,
                            data,
                            request,
                            response,
                            language,
                            server,
                            os,
                            additionalFieldsFromSettings,
                            SdkToken);
                    };

                    var subscription = HttpContext.Current.AddOnRequestCompleted(AddOnRequestCompletedCallback);
                }
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("OnActionExecuted", ex);
            }
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