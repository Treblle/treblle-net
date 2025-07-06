using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Web;
using Treblle.Net;
using Treblle.Net.Helpers;

public class TreblleSyncOperationInvoker : IOperationInvoker
{
    private readonly IOperationInvoker _innerInvoker;
    public string ApiKey = "";
    public string ProjectId = "";
    TrebllePayload payload = new TrebllePayload();
    Data data = new Data();
    Response response = new Response();
    Request request = new Request();
    Language language = new Language();
    Server server = new Server();
    Os os = new Os();

    public TreblleSyncOperationInvoker(IOperationInvoker inner)
    {
        _innerInvoker = inner;
    }

    public object[] AllocateInputs() => _innerInvoker.AllocateInputs();

    public bool IsSynchronous => true;

    public object Invoke(object instance, object[] inputs, out object[] outputs)
    {
        
        var stopwatch = Stopwatch.StartNew();
        outputs = null;

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
                request = HttpContextHelper.ExtractRequestData(HttpContext.Current, WebOperationContext.Current.IncomingRequest);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Treblle pre-processing failed: " + ex.Message);
        }

        try
        {
            var result = _innerInvoker.Invoke(instance, inputs, out outputs);

            stopwatch.Stop();
            response.LoadTime = stopwatch.ElapsedMilliseconds;
            response.Body = result;
            response.Code = HttpContext.Current?.Response?.StatusCode ?? 200;
            response.Headers = HttpContext.Current?.Response?.Headers.AllKeys?
                .ToDictionary(k => k, k => HttpContext.Current.Response.Headers[k]);

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
            
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            response.LoadTime = stopwatch.ElapsedMilliseconds;
            response.Code = 500;
            response.Body = null;
            response.Size = 0;

            var error = HttpContextHelper.ExtractErrorData(ex);
            data.Errors = new List<Error>();
            data.Errors.Add(error);

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
            throw;
        }
    }

    public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        => throw new NotSupportedException("Only synchronous operations are supported.");

    public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        => throw new NotSupportedException("Only synchronous operations are supported.");


}