using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using System.Configuration;

namespace WebApi
{
    public class ConfigurationSource :
       IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class, new()
        {
            if (typeof(T) == typeof(GatewayConfig))
            {
                var gatewayConfig = new GatewayConfig
                {
                    Channels = new ChannelCollection { new ChannelConfig
                        {
                            ChannelType = "Http",
                            Address = "http://localhost:25000/Fueling/",
                            Default = true
                        }
                    }
                };

                return gatewayConfig as T;
            }

            // Respect app.config for other sections not defined in this method
            return ConfigurationManager.GetSection(typeof(T).Name) as T;
        }
    }
}
