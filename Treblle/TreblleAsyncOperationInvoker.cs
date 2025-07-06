using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Dispatcher;
using System.ServiceModel.Web;
using System.Threading;
using System.Web;
using Treblle.Net.Helpers;

namespace Treblle.Net
{
    public class TreblleAsyncOperationInvoker : IOperationInvoker
    {
        private readonly IOperationInvoker _innerInvoker;
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

        public TreblleAsyncOperationInvoker(IOperationInvoker inner)
        {
            _innerInvoker = inner;
        }

        public object[] AllocateInputs() => _innerInvoker.AllocateInputs();

        public bool IsSynchronous => false;

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            stopwatch = Stopwatch.StartNew();

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

            // Start async call
            var innerAsyncResult = _innerInvoker.InvokeBegin(instance, inputs, ar =>
            {
                // Invoke outer callback
                callback?.Invoke(ar);
            }, state);

            return new AsyncResultWrapper(innerAsyncResult, state);
        }

        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            ApiKey = ConfigurationManager.AppSettings["TreblleApiKey"];
            ProjectId = ConfigurationManager.AppSettings["TreblleProjectId"];
            outputs = null;

            try
            {
                var returnValue = _innerInvoker.InvokeEnd(instance, out outputs, result);
                stopwatch.Stop();

                var response = new Response
                {
                    LoadTime = stopwatch.ElapsedMilliseconds,
                    Body = returnValue,
                    Code = HttpContext.Current?.Response?.StatusCode ?? 200,
                    Headers = HttpContext.Current?.Response?.Headers?.AllKeys?
                        .ToDictionary(k => k, k => HttpContext.Current.Response.Headers[k])
                };

                var trebbleSender = new TrebllePayloadSender();
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
                                ApiKey); return returnValue;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                var response = new Response
                {
                    LoadTime = stopwatch.ElapsedMilliseconds,
                    Code = 500,
                    Body = null,
                    Headers = null
                };

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

        public object Invoke(object instance, object[] inputs, out object[] outputs)
            => throw new NotSupportedException("Synchronous Invoke is not supported here");

    }

    public class AsyncResultWrapper : IAsyncResult
    {
        private readonly IAsyncResult _inner;
        private readonly object _state;

        public AsyncResultWrapper(IAsyncResult inner, object state)
        {
            _inner = inner;
            _state = state;
        }

        public object AsyncState => _state;
        public WaitHandle AsyncWaitHandle => _inner.AsyncWaitHandle;
        public bool CompletedSynchronously => _inner.CompletedSynchronously;
        public bool IsCompleted => _inner.IsCompleted;
    }
}
