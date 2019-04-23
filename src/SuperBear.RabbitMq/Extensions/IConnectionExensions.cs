using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SuperBear.RabbitMq.Build;

namespace SuperBear.RabbitMq.Extensions
{
    // ReSharper disable once InconsistentNaming
    public static class IConnectionExensions
    {
        public static Channel CreateChannel(this Factory connection)
        {
            return new Channel()
            {
                CurrentChannel = connection.CurrentConnection.CreateModel(),
                Logger = connection.Logger
            };
        }
    }
}
