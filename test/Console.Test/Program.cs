using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Framing;
using SuperBear.RabbitMq;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Extensions;
using Consolea = System.Console;


namespace Console.Test
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static void Main(string[] args)
        {
            InitConfigurationManager();
            var serviceProvider = InitDependencyInjection();
            InitRabbitMq();
            var factory = (Factory)serviceProvider.GetService(typeof(Factory));
            var channel = factory.CreateChannel();

            var basicProperties = channel.CreateBasicProperties(new SuperBear.RabbitMq.Build.BasicProperties() { Persistent = true });
            channel.Publish(basicProperties, "asd", new PublicationAddress(ExchangeType.Direct, "Exchange", "RoutingKey"));
            channel.Receive<List<string>>((message, ea) =>
            {
                throw new Exception("");
            }, "Queue");
        }

        public static void InitRabbitMq()
        {
            SuperBear.RabbitMq.Init.Initialize.Init(config =>
            {
                MessageStructure scheduler = new MessageStructure()
                {
                    Exchange = new Exchange()
                    {
                        Durable = true,
                        AutoDelete = false,
                        Name = $"Exchange",
                        Type = ExchangeTypeEnum.Direct
                    },
                    Queue = new Queue()
                    {
                        Name = $"Queue",
                        Durable = true,
                        AutoDelete = false,
                        Exclusive = false
                    },
                    RoutingKey = $"RoutingKey"
                };
                config.InitMessageStructure(scheduler);
            });
        }
        public static IServiceProvider InitDependencyInjection()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });
            services.AddRabbitMq(option =>
            {
                option.UserName = Configuration["Omega:RabbitOption:UserName"];
                option.Password = Configuration["Omega:RabbitOption:Password"];
                option.HostName = Configuration["Omega:RabbitOption:HostName"];
                option.EnvironmentName = Configuration["Omega:RabbitOption:EnvironmentName"];
                option.AdditionalConfig = new RabbitMqAdditionalConfig() { AutomaticRecoveryEnabled = true };
                option.ManagePort = "15672";
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
