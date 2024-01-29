using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Xml.Linq;

namespace Treblle.Net
{
    public class TreblleAttribute : ActionFilterAttribute
    {
        private static readonly HttpClient Client = new HttpClient();

        private static readonly List<string> SensitiveWords = new List<string>
        {
          "password",
          "pwd",
          "secret",
          "password_confirmation",
          "passwordConfirmation",
          "cc",
          "card_number",
          "cardNumber",
          "ccv",
          "ssn",
          "credit_score",
          "creditScore"
        };

        public string ApiKey = "";

        public string ProjectId = "";

        Stopwatch stopwatch = new Stopwatch();

        TrebllePayload payload = new TrebllePayload();
        Data data = new Data();
        Response response = new Response();
        Error error = new Error();
        Request request = new Request();
        Language language = new Language();
        Server server = new Server();
        Os os = new Os();

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            try
            {
                ApiKey = System.Configuration.ConfigurationManager.AppSettings["TreblleApiKey"];
                ProjectId = System.Configuration.ConfigurationManager.AppSettings["TreblleProjectId"];

                if (!string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId))
                {
                    stopwatch.Start();

                    payload.Sdk = "net";
                    payload.Version = actionContext.Request.Version.ToString();
                    payload.ProjectId = ProjectId;
                    payload.ApiKey = ApiKey;

                    language.Name = "c#";

                    if(Environment.Version != null)
                    {
                        language.Version = $"{Environment.Version.Major}.{Environment.Version.Minor}.{Environment.Version.Revision}";
                    }
                    else
                    {
                        language.Version = String.Empty;
                    }

                    server.Ip = HttpContext.Current.Request.ServerVariables["LOCAL_ADDR"];
                    server.Timezone = (!String.IsNullOrEmpty(TimeZone.CurrentTimeZone.StandardName)) ? TimeZone.CurrentTimeZone.StandardName : "UTC";
                    server.Software = HttpContext.Current.Request.ServerVariables["SERVER_SOFTWARE"];
                    server.Signature = null;
                    server.Protocol = HttpContext.Current.Request.ServerVariables["SERVER_PROTOCOL"];

                    os.Name = Environment.OSVersion.ToString();
                    os.Release = Environment.OSVersion.Version.ToString();
                    os.Architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");

                    request.Timestamp = HttpContext.Current.Request.RequestContext.HttpContext.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    request.Ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
                    request.Url = actionContext.Request.RequestUri.AbsoluteUri;
                    request.UserAgent = actionContext.Request.Headers.UserAgent.ToString();
                    request.Method = actionContext.Request.Method.ToString();

                    request.Body = null;

                    if (actionContext.Request.Content.Headers.ContentType != null)
                    {
                        if (actionContext.Request.Content.Headers.ContentType.ToString().Contains("application/json"))
                        {
                            Stream req = HttpContext.Current.Request.InputStream;
                            req.Seek(0, SeekOrigin.Begin);
                            var bodyJson = new StreamReader(req).ReadToEnd();

                            request.Body = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(bodyJson);
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
                            string jsonText = Newtonsoft.Json.JsonConvert.SerializeXNode(doc);
                            request.Body = Newtonsoft.Json.JsonConvert.DeserializeObject<ExpandoObject>(jsonText);
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
                        }
                    }
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
                ApiKey = System.Configuration.ConfigurationManager.AppSettings["TreblleApiKey"];
                ProjectId = System.Configuration.ConfigurationManager.AppSettings["TreblleProjectId"];

                if (!string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(ProjectId))
                {
                    data.Errors = new List<Error>();

                    if (actionExecutedContext.Exception != null)
                    {

                        error.Source = "onException";
                        error.Type = actionExecutedContext.Exception.GetType().Name;
                        error.Message = actionExecutedContext.Exception.Message;
                        error.File = null;
                        error.Line = 0;

                        var stackTrace = new StackTrace(actionExecutedContext.Exception, true);
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
                                var outputStream = actionExecutedContext.Response.Content.ReadAsStreamAsync().Result;

                                outputStream.Seek(0, SeekOrigin.Begin);

                                var outputBody = new StreamReader(outputStream).ReadToEnd();

                                response.Body = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(outputBody);
                                response.Size = actionExecutedContext.Response.Content.Headers.ContentLength.HasValue ? actionExecutedContext.Response.Content.Headers.ContentLength.Value : 0;
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                    stopwatch.Stop();
                    response.LoadTime = stopwatch.ElapsedMilliseconds / (double)1000;

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

                            }
                        }
                        PrepareAndSendJson();
                    };

                    var subscription = HttpContext.Current.AddOnRequestCompleted(AddOnRequestCompletedCallback);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void PrepareAndSendJson()
        {

            server.Os = os;

            data.Language = language;
            data.Request = request;
            data.Response = response;
            data.Server = server;

            payload.Data = data;

            var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);

            var additionalFieldsToMask = System.Configuration.ConfigurationManager.AppSettings["AdditionalFieldsToMask"];
            if (!string.IsNullOrWhiteSpace(additionalFieldsToMask))
            {
                var additionalFields = additionalFieldsToMask.Split(',');
                if (additionalFields.Any())
                {
                    var list = additionalFields.ToList();
                    SensitiveWords.AddRange(list);
                }
            }
            
            var maskedJson = json.Mask(SensitiveWords.ToArray(), "*****");

            using (var content = new StringContent(maskedJson, Encoding.UTF8, "application/json"))
            {
                content.Headers.Add("x-api-key", ApiKey);

                var httpResponseMessage = Client
                    .PostAsync("https://rocknrolla.treblle.com", content)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}