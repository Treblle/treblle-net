using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Treblle.Net.Helpers;

namespace Treblle.Net
{
    public class TreblleAttribute : ActionFilterAttribute
    {

        public string ApiKey = "";
        public string ProjectId = "";

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
                ApiKey = ConfigurationManager.AppSettings["TreblleApiKey"];
                ProjectId = ConfigurationManager.AppSettings["TreblleProjectId"];

                if (!string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId))
                {
                    stopwatch.Start();

                    payload = HttpContextHelper.ExtractTrebllePayloadData(ProjectId, ApiKey);
                    language = EnvironmentHelper.ExtractLanguageData();
                    server = HttpContextHelper.ExtractServerData(HttpContext.Current.Request);
                    os = EnvironmentHelper.ExtractOsData();
                    request = HttpContextHelper.ExtractRequestData(HttpContext.Current, actionContext);                  
                }
            }
            catch (Exception ex)
            {
            }

            base.OnActionExecuting(actionContext);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            try
            {
                ApiKey = ConfigurationManager.AppSettings["TreblleApiKey"];
                ProjectId = ConfigurationManager.AppSettings["TreblleProjectId"];

                if (!string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId))
                {
                    data.Errors = new List<Error>();

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
                                if (actionExecutedContext.Response.Content.Headers.ContentLength.HasValue && actionExecutedContext.Response.Content.Headers.ContentLength.Value > 2048)
                                {
                                    payload.Data.Errors.Add(new Error
                                    {
                                        Message = "JSON response size is over 2MB",
                                        Type = "E_USER_ERROR",
                                        File = string.Empty,
                                        Line = 0
                                    });
                                }
                                else
                                {
                                    var outputStream = actionExecutedContext.Response.Content.ReadAsStreamAsync().Result;

                                    outputStream.Seek(0, SeekOrigin.Begin);

                                    var outputBody = new StreamReader(outputStream).ReadToEnd();

                                    if (IsValidJson(outputBody))
                                    {
                                        response.Body = JsonConvert.DeserializeObject<dynamic>(outputBody);
                                    }
                                    else
                                    {
                                        Console.WriteLine("Invalid JSON in response");
                                    }
                                    response.Size = actionExecutedContext.Response.Content.Headers.ContentLength.HasValue ? actionExecutedContext.Response.Content.Headers.ContentLength.Value : 0;
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
                                Console.WriteLine(ex.Message);
                            }
                        }

                        string additionalFieldsFromSettings = ConfigurationManager.AppSettings["FieldsToMaskPairedWithMaskers"];
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
                            ApiKey);
                    };

                    var subscription = HttpContext.Current.AddOnRequestCompleted(AddOnRequestCompletedCallback);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
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