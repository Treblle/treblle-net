using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Treblle.Net
{
    public class TreblleOperationBehavior : IOperationBehavior
    {
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            dispatchOperation.Invoker = new TreblleSyncOperationInvoker(dispatchOperation.Invoker);
        }

        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }
        public void Validate(OperationDescription operationDescription) { }
    }
}