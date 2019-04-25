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
            factory.CurrentConnection.CreateChannel();
        }

        [Fact]
        public void Test_Init_Queue()
        {
            var factory = (Factory)ServiceProvider.GetService(typeof(Factory));
            Initialize.Init(config =>
            {
                config.InitExchange(new Exchange("asdasdasd"));
                config.InitQueue(new Queue("abcde"));
                config.InitBind(new Exchange("asdasdasd"), new Queue("abcde"), "asdasd");
            }, factory);
        }
    }
}
