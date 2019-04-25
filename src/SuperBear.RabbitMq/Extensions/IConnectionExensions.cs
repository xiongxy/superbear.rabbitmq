using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Init;

namespace SuperBear.RabbitMq.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class IConnectionExensions
    {
        public static Channel CreateChannel(this IConnection connection)
        {
            var channel = new Channel()
            {
                CurrentChannel = connection.CreateModel(),
                Logger = new LoggerFactory().CreateLogger("SuperBear.RabbitMq")
            };
            Initialize.Init(channel);
            return channel;
        }
    }
}
