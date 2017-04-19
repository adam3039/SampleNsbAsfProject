using Microsoft.ServiceFabric.Services.Communication.Runtime;
using NServiceBus;
using NServiceBus.Features;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Fabric;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Receiver.GatewayConfig
{
    internal class NServiceBusGatewayCommunicationListener : ICommunicationListener
    {
        private readonly ServiceEventSource _eventSource;
        private readonly ServiceContext _serviceContext;
        private readonly string _endpointName;
        private readonly string _appRoot;

        private string _publishAddress;
        private string _listeningAddress;
        private IEndpointInstance _endpoint;

        public NServiceBusGatewayCommunicationListener(ServiceContext serviceContext, ServiceEventSource eventSource, string endpointName)
            : this(serviceContext, eventSource, endpointName, null)
        {
        }

        public NServiceBusGatewayCommunicationListener(ServiceContext serviceContext, ServiceEventSource eventSource, string endpointName, string appRoot)
        {

            if (serviceContext == null)
            {
                throw new ArgumentNullException(nameof(serviceContext));
            }

            if (endpointName == null)
            {
                throw new ArgumentNullException(nameof(endpointName));
            }

            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            _serviceContext = serviceContext;
            _endpointName = endpointName;
            _eventSource = eventSource;
            _appRoot = appRoot;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            var serviceEndpoint = _serviceContext.CodePackageActivationContext.GetEndpoint(_endpointName);
            var protocol = serviceEndpoint.Protocol;
            var port = serviceEndpoint.Port;

            if (!(_serviceContext is StatelessServiceContext))
            {
                throw new InvalidOperationException("Should only be used with Stateless services.");
            }

            _listeningAddress = string.Format(
                CultureInfo.InvariantCulture,
                "{0}://+:{1}/{2}",
                protocol,
                port,
                string.IsNullOrWhiteSpace(_appRoot)
                    ? string.Empty
                    : _appRoot.TrimEnd('/') + '/');

            _publishAddress = _listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            try
            {
                _eventSource.Message("Receiver gateway endpoint starting");
                var endpointConfig = ConfigurateGatewayEndpoint();
                _endpoint = await Endpoint.Start(endpointConfig).ConfigureAwait(false);
                _eventSource.Message("Receiver gateway endpoint started");

                return _publishAddress;
            }
            catch (Exception ex)
            {
                _eventSource.Message("Failed to start an endpoint. {0}", ex.ToString());

                await StopEndpointAsync().ConfigureAwait(false);

                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _eventSource.Message("Closing endpoint");
            return StopEndpointAsync();
        }

        public void Abort()
        {
            _eventSource.Message("Aborting endpoint");
            StopEndpointAsync().GetAwaiter().GetResult();
        }

        private Task StopEndpointAsync()
        {
            if (_endpoint != null)
            {
                return _endpoint.Stop();
            }

            return Task.CompletedTask;
        }

        private EndpointConfiguration ConfigurateGatewayEndpoint()
        {
            var endpointConfiguration = new EndpointConfiguration(ConfigurationManager.AppSettings["GatewayQueue"]);
            endpointConfiguration.License("<?xml version=\"1.0\" encoding=\"utf-8\"?><license id=\"5049ba14-3897-42e5-a4ad-1ab821a71b59\" expiration=\"2117-02-08T17:06:14.8037759\" type=\"Standard\" ProductName=\"Royalty Free Platform License\" WorkerThreads=\"Max\" LicenseVersion=\"6.0\" MaxMessageThroughputPerSecond=\"Max\" AllowedNumberOfWorkerNodes=\"Max\" UpgradeProtectionExpiration=\"2018-02-08\" Applications=\"NServiceBus;ServiceControl;ServicePulse;\" LicenseType=\"Royalty Free Platform License\" Perpetual=\"\" Quantity=\"1\" Edition=\"Advanced \"><name>Fireball Equipment Ltd.</name><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>C+x6ThL1MKe3kNFg0xLl+B26kJc=</DigestValue></Reference></SignedInfo><SignatureValue>la5SZt2IXGPYIl/ImPo4tBZxXDiWgrJ+vmE/07R571Z8BrOeYOmxzd0Y8s9GlEirGW1RbGNBvNxVe63zHG67SzmdI9J12N7QKmsJuRr14/yEMO2dZWMjivfqft23nVqEhyB2he+blW8tXzLW/xShBHQW+6BF4EijRkAuTeMLQkY=</SignatureValue></Signature></license>");
            endpointConfiguration.EnableFeature<Gateway>();

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.UseForwardingTopology();
            transport.ConnectionString(ConfigurationManager.AppSettings["ServiceBusConnectionStr"]);

            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.UseSerialization<XmlSerializer>();

            endpointConfiguration.SendFailedMessagesTo(ConfigurationManager.AppSettings["ErrorQueue"]);
            endpointConfiguration.AuditProcessedMessagesTo(ConfigurationManager.AppSettings["AuditQueue"]);

            //Conventions required for Shared Project instead of PCL
            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningMessagesAs(type => type.Namespace?.StartsWith("Contracts.Msgs") ?? false);

            return endpointConfiguration;
        }
    }
}
