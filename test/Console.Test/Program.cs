using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SuperBear.RabbitMq;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Extensions;


namespace Console.Test
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static void Main(string[] args)
        {
            InitConfigurationManager();
            var serviceProvider = InitDependencyInjection();
            var factory = (Factory)serviceProvider.GetService(typeof(Factory));
            var channel = factory.CurrentConnection.CreateChannel();
            channel.SetPrefetch(1);
            channel.DefineExchange(new Exchange()
            {
                Name = "Exchange",
                Type = ExchangeTypeEnum.Direct
            })
            .DefineQueue(new Queue()
            {
                Name = "Queue",
                Retry = true
            })
            .Bind("routingKey").Commit();

            var basicProperties = channel.CretaeBasicProperties(new BasicProperties());

            for (int i = 0; i < 1; i++)
            {
                channel.Publish(basicProperties, $"asd{i}");
            }

            channel.Receive<string>((item, ea) =>
            {
                throw new Exception("asd");
            });

            System.Console.ReadLine();
        }
        public static IServiceProvider InitDependencyInjection()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddRabbitMq(option =>
            {
                option.UserName = Configuration["Omega:RabbitOption:UserName"];
                option.Password = Configuration["Omega:RabbitOption:Password"];
                option.HostName = Configuration["Omega:RabbitOption:HostName"];
                option.EnvironmentName = Configuration["Omega:RabbitOption:EnvironmentName"];
                option.AdditionalConfig = new RabbitMqAdditionalConfig() { AutomaticRecoveryEnabled = true };
            });
            services.AddSingleton(Configuration);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            return serviceProvider;
        }
        public static void InitConfigurationManager()
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");
            Configuration = builder.Build();
        }
    }
}
