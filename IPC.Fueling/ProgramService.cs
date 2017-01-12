using System;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Features;
using Shared;
using IPC.Fueling;
using System.Net;

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
            var endpointConfiguration = new EndpointConfiguration("IPC.Fueling");
            endpointConfiguration.UseSerialization<JsonSerializer>();
            endpointConfiguration.SendFailedMessagesTo("error");
            endpointConfiguration.EnableFeature<Gateway>();
            endpointConfiguration.Gateway();
            endpointConfiguration.CustomConfigurationSource(new ConfigurationSource());
            endpointConfiguration.AuditProcessedMessagesTo("audit");
            endpointConfiguration.DefineCriticalErrorAction(OnCriticalError);
            //TODO: this if is here to prevent accidentally deploying to production without considering important actions
            if (Environment.UserInteractive && Debugger.IsAttached)
            {
                //TODO: For production use select a durable persistence.
                // https://docs.particular.net/nservicebus/persistence/
                endpointConfiguration.UsePersistence<InMemoryPersistence>();

                //TODO: For production use script the installation.
                endpointConfiguration.EnableInstallers();
            }
            endpointConfiguration.UsePersistence<InMemoryPersistence>();
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
                "RemoteEndpoint"
            }, new AsbMessage())
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