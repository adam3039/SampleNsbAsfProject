using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using System;
using System.Configuration;

namespace Sender
{
    public class ConfigurationSource :
        IConfigurationSource

    {
        public T GetConfiguration<T>() where T : class, new()
        {
            if (typeof(T) == typeof(GatewayConfig))
            {
                string gatewayURL;
                var settings = ConfigurationManager.AppSettings;
                if (settings["UseLocalHostForGateway"] == "true")
                {
                    gatewayURL = $"http://{Environment.MachineName}{settings["GatewayPortAndPath"]}"; //http://netbiosname:port/Path/
                }
                else
                {
                    gatewayURL = $"{settings["GatewayProtoAndUrl"]}{settings["GatewayPortAndPath"]}"; //http://settings.url:port/Path/
                }
                var gatewayConfig = new GatewayConfig
                {
                    Sites = new SiteCollection
                    {
                        new SiteConfig
                        {
                            Key = "receiver",
                            Address = ConfigurationManager.AppSettings["GatewayUrl"],
                            ChannelType = "Http"
                        }
                    },
                    Channels = new ChannelCollection { new ChannelConfig
                        {
                            ChannelType = "Http",
                            Address = gatewayURL,
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
