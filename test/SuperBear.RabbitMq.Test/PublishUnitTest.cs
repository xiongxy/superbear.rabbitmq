using System;
using RabbitMQ.Client;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Extensions;
using SuperBear.RabbitMq.Init;
using Xunit;

namespace SuperBear.RabbitMq.Test
{
    public class PublishUnitTest : TestBase
    {
        [Fact]
        public void Test_Publish()
        {
            var factory = (Factory)ServiceProvider.GetService(typeof(Factory));
            var channel = factory.CreateChannel();
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
            var properties = channel.CreateBasicProperties(new BasicProperties() { Persistent = true });
            channel.Publish(properties, "abcd", new PublicationAddress(ExchangeType.Direct, "Exchange", "routingKey"));
        }
    }
}
