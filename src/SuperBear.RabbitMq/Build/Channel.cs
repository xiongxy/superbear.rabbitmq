using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace SuperBear.RabbitMq.Build
{
    public class Channel : IDisposable
    {
        public IModel CurrentChannel { get; set; }
        public Exchange Exchange { get; set; } 
        public Queue Queue { get; set; }
        public string RoutingKey { get; set; }
        public void Dispose()
        {
            CurrentChannel?.Dispose();
        }
    }
}
