using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
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
            var factory = (Factory)serviceProvider.GetService(typeof(Factory));
            var channel = factory.CurrentConnection.CreateChannel();

            channel.SetPrefetch(1);

            new MessageStructure()
            {
                Exchange = new Exchange()
                {
                    Name = "Exchange",
                    Type = ExchangeTypeEnum.Direct
                },
                Queue = new Queue()
                {
                    Name = "Queue"
                },
                RoutingKey = "routingKey"
            }.Commit(channel);

            var basicProperties = channel.CreateBasicProperties(new BasicProperties()
            {
                Persistent = true 
            });

            //for (int i = 0; i < 1000000; i++)
            //{
            //    channel.Publish(basicProperties, $"asd{i}", new PublicationAddress(ExchangeTypeEnum.Direct.ToString().ToLower(), "Exchange", "routingKey"));
            //}

            channel.Receive<string>((item, ea) =>
            {
                //Consolea.WriteLine(JsonConvert.SerializeObject(item));
            }, "Queue");
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
