using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Receiver.GatewayConfig
{
    public class ConfigureGateway : IProvideConfiguration<NServiceBus.Config.GatewayConfig>
    {
        public NServiceBus.Config.GatewayConfig GetConfiguration()
        {
            return new NServiceBus.Config.GatewayConfig
            {
                TransactionTimeout = TimeSpan.FromMinutes(10),
                Channels = new ChannelCollection
                {
                    new ChannelConfig
                    {
                        Address = "http://+:25000/receiver/",
                        ChannelType = "http",
                        Default = true
                    }
                }
            };
        }
    }
}
