using Contracts.Msgs;
using NServiceBus;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Receiver.Handlers
{
    public class MessageHandler : IHandleMessages<SampleMessage>
    {
        public Task Handle(SampleMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine("Message Received");
            return Task.CompletedTask;
        }
    }
}
