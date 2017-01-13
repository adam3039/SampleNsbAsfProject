using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using NServiceBus;
using NServiceBus.Features;
using WebApi.Handlers;

namespace WebApi
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class WebApi : StatelessService
    {
        public WebApi(StatelessServiceContext context)
            : base(context)
        {
            var endpointConfiguration = new EndpointConfiguration("RemoteEndpoint");
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.EnableFeature<Gateway>();
            var gateway = endpointConfiguration.Gateway();
            endpointConfiguration.CustomConfigurationSource(new ConfigurationSource());
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.RegisterComponents(components =>
            {
                components.ConfigureComponent(builder =>
                {
                    return new TransactionHandler
                    {
                        ServiceContext = context
                    };
                }, DependencyLifecycle.SingleInstance);
            });

            var endpoint = Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext => new OwinCommunicationListener(Startup.ConfigureApp, serviceContext, ServiceEventSource.Current, "ServiceEndpoint"), "ApiListener")
                //,
                //new ServiceInstanceListener(context => new HttpCommunicationListener(context,ServiceEventSource.Current),"RemoteListener")
            };
        }
    }
}
