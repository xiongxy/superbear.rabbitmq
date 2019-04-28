using System;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Extensions;
using SuperBear.RabbitMq.Init;
using Xunit;

namespace SuperBear.RabbitMq.Test
{
    public class InitUnitTest : TestBase
    {
        [Fact]
        public void Test_Init_RabbitMq()
        {
            var factory = (Factory)ServiceProvider.GetService(typeof(Factory));
            factory.CreateChannel();
        }

        [Fact]
        public void Test_Init_Queue()
        {
            Initialize.Init(config =>
            {
                MessageStructure messageStructure = new MessageStructure
                {
                    Exchange = new Exchange(),
                    Queue = new Queue(),
                    RoutingKey = Guid.NewGuid().ToString("N")
                };
                config.InitMessageStructure(messageStructure);
            });
            var factory = (Factory)ServiceProvider.GetService(typeof(Factory));
            factory.CreateChannel();
        }
    }
}
