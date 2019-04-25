using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace SuperBear.RabbitMq.Test
{
    public class TestBase
    {
        public IServiceProvider ServiceProvider { get; } = InitDependencyInjection();
        public static IServiceProvider InitDependencyInjection()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddRabbitMq(option =>
            {
                option.UserName = "guest";
                option.Password = "guest";
                option.HostName = "192.168.200.138";
                option.EnvironmentName = "DEBUG";
                option.AdditionalConfig = new RabbitMqAdditionalConfig() { AutomaticRecoveryEnabled = true };
                option.ManagePort = "35672";
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
    }
}
