using NServiceBus;
using NServiceBus.Logging;
using Shared;
using System.Fabric;
using System.Threading.Tasks;

namespace WebApi.Handlers
{
    public class TransactionHandler : IHandleMessages<AsbMessage>
    {
        public StatelessServiceContext ServiceContext { get; set; }

        static ILog log = LogManager.GetLogger<TransactionHandler>();

        public Task Handle(AsbMessage message, IMessageHandlerContext context)
        {
            ServiceEventSource.Current.ServiceMessage(ServiceContext, "Transaction message received");
            return Task.CompletedTask;
        }
    }
}
