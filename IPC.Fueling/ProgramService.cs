using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Features;
using System.Net;
using Contracts.Msgs;
using Sender;

[DesignerCategory("Code")]
class ProgramService : ServiceBase
{
    IEndpointInstance endpoint;

    static ILog logger = LogManager.GetLogger<ProgramService>();

    static void Main()
    {
        using (var service = new ProgramService())
        {
            // to run interactive from a console or as a windows service
            if (Environment.UserInteractive)
            {
                Console.CancelKeyPress += (sender, e) =>
                {
                    service.OnStop();
                };
                service.OnStart(null);
                Console.WriteLine("\r\nPress enter key to stop program\r\n");
                Console.Read();
                service.OnStop();
                return;
            }
            Run(service);
        }
    }

    protected override void OnStart(string[] args)
    {
        AsyncOnStart().GetAwaiter().GetResult();
    }

    async Task AsyncOnStart()
    {
        try
        {
            var endpointConfiguration = new EndpointConfiguration("Sender");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.EnableFeature<Gateway>();
            endpointConfiguration.Gateway();
            endpointConfiguration.CustomConfigurationSource(new ConfigurationSource());
            endpointConfiguration.License("<?xml version=\"1.0\" encoding=\"utf-8\"?><license id=\"5049ba14-3897-42e5-a4ad-1ab821a71b59\" expiration=\"2117-02-08T17:06:14.8037759\" type=\"Standard\" ProductName=\"Royalty Free Platform License\" WorkerThreads=\"Max\" LicenseVersion=\"6.0\" MaxMessageThroughputPerSecond=\"Max\" AllowedNumberOfWorkerNodes=\"Max\" UpgradeProtectionExpiration=\"2018-02-08\" Applications=\"NServiceBus;ServiceControl;ServicePulse;\" LicenseType=\"Royalty Free Platform License\" Perpetual=\"\" Quantity=\"1\" Edition=\"Advanced \"><name>Fireball Equipment Ltd.</name><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignedInfo><CanonicalizationMethod Algorithm=\"http://www.w3.org/TR/2001/REC-xml-c14n-20010315\" /><SignatureMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#rsa-sha1\" /><Reference URI=\"\"><Transforms><Transform Algorithm=\"http://www.w3.org/2000/09/xmldsig#enveloped-signature\" /></Transforms><DigestMethod Algorithm=\"http://www.w3.org/2000/09/xmldsig#sha1\" /><DigestValue>C+x6ThL1MKe3kNFg0xLl+B26kJc=</DigestValue></Reference></SignedInfo><SignatureValue>la5SZt2IXGPYIl/ImPo4tBZxXDiWgrJ+vmE/07R571Z8BrOeYOmxzd0Y8s9GlEirGW1RbGNBvNxVe63zHG67SzmdI9J12N7QKmsJuRr14/yEMO2dZWMjivfqft23nVqEhyB2he+blW8tXzLW/xShBHQW+6BF4EijRkAuTeMLQkY=</SignatureValue></Signature></license>");
            var conventions = endpointConfiguration.Conventions();
            conventions.DefiningMessagesAs(type => type.Namespace?.StartsWith("Contracts.Msgs") ?? false);
            endpointConfiguration.UseSerialization<XmlSerializer>();

            endpoint = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);
            await PerformStartupOperations(endpoint);
        }
        catch (Exception exception)
        {
            logger.Fatal("Failed to start", exception);
            Environment.FailFast("Failed to start", exception);
        }
    }

    async Task PerformStartupOperations(IEndpointInstance endpoint)
    {
        await endpoint.SendToSites(new[]
            {
                "receiver"
            }, new SampleMessage())
            .ConfigureAwait(false);
    }

    Task OnCriticalError(ICriticalErrorContext context)
    {
        //TODO: Decide if shutting down the process is the best response to a critical error
        // https://docs.particular.net/nservicebus/hosting/critical-errors
        var fatalMessage = $"The following critical error was encountered:\n{context.Error}\nProcess is shutting down.";
        logger.Fatal(fatalMessage, context.Exception);
        Environment.FailFast(fatalMessage, context.Exception);
        return Task.FromResult(0);
    }

    protected override void OnStop()
    {
        endpoint?.Stop().GetAwaiter().GetResult();
    }
}