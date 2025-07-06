using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Treblle.Net
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class TreblleBehaviorAttribute : Attribute, IServiceBehavior, IOperationBehavior
    {
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (var endpoint in serviceDescription.Endpoints)
            {
                foreach (var operation in endpoint.Contract.Operations)
                {
                    // Attach this as an IOperationBehavior to each operation
                    if (!operation.Behaviors.Contains(this))
                    {
                        operation.Behaviors.Add(this);
                    }
                }
            }
        }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }


        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            //var method = operationDescription.SyncMethod ?? operationDescription.BeginMethod;

            var isAsync = operationDescription.TaskMethod != null;

            if (isAsync)
            {
                dispatchOperation.Invoker = new TreblleAsyncOperationInvoker(dispatchOperation.Invoker);
            }
            else
            {
                dispatchOperation.Invoker = new TreblleSyncOperationInvoker(dispatchOperation.Invoker);
            }
        }
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }
        public void Validate(OperationDescription operationDescription) { }
    }

}
