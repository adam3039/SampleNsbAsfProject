using Microsoft.ServiceFabric.Services.Communication.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Fabric.Description;
using System.Net;
using System.Fabric;

namespace WebApi
{
    class HttpCommunicationListener : ICommunicationListener
    {
        private readonly HttpListener _httpListener;
        private readonly StatelessServiceContext _serviceContext;
        private readonly ServiceEventSource _eventSource;
        private readonly Func<HttpListenerContext, CancellationToken, Task> _processRequest;
        private readonly CancellationTokenSource _processRequestsCancellation = new CancellationTokenSource();

        public HttpCommunicationListener(StatelessServiceContext serviceContext, ServiceEventSource eventSource)
        {
            _serviceContext = serviceContext;
            _httpListener = new HttpListener();
            _eventSource = eventSource;
        } 

        public void Abort()
        {
            _httpListener.Abort();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            _httpListener.Close();
            return Task.FromResult(true);
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            EndpointResourceDescription endpoint =
                 _serviceContext.CodePackageActivationContext.GetEndpoint("RemoteEndpoint");

            string uriPrefix = $"{endpoint.Protocol}://+:{endpoint.Port}/Fueling/";

            _httpListener.Prefixes.Add(uriPrefix);
            _httpListener.Start();

            string uriPublished = uriPrefix.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

            _eventSource.ServiceMessage(_serviceContext, "Remote listener started on: " + uriPublished);

            while (!cancellationToken.IsCancellationRequested)
            {
                HttpListenerContext request = await _httpListener.GetContextAsync();

                using (HttpListenerResponse response = request.Response)
                {
                    var output = "test response";
                    if (output != null)
                    {
                        response.ContentType = "text/html";
                        byte[] outBytes = Encoding.UTF8.GetBytes(output);
                        response.OutputStream.Write(outBytes, 0, outBytes.Length);
                    }
                }
            }


            return uriPublished;
        }
    }
}
