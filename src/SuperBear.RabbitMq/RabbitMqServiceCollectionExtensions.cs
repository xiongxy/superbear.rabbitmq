using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace SuperBear.RabbitMq
{
    public static class RabbitMqServiceCollectionExtensions
    {
        public static IServiceCollection AddRabbitMq(this IServiceCollection services, Action<RabbitOption> option)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (option == null)
                throw new ArgumentNullException(nameof(option));
            services.AddOptions();
            services.Configure(option);
            services.AddLogging();
            services.AddSingleton<Factory>();
            return services;
        }
    }
}
