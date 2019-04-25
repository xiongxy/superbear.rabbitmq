using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using SuperBear.RabbitMq.Build;

namespace SuperBear.RabbitMq
{
    internal static class MemoryMap
    {
        static MemoryMap()
        {
            Exchanges = new ConcurrentDictionary<string, Exchange>();
            Queues = new ConcurrentDictionary<string, Queue>();
            BindRelationship = new ConcurrentBag<BindRelationship>();
        }
        internal static System.Collections.Concurrent.ConcurrentDictionary<string, Exchange> Exchanges { get; set; }
        internal static System.Collections.Concurrent.ConcurrentDictionary<string, Queue> Queues { get; set; }
        internal static System.Collections.Concurrent.ConcurrentBag<BindRelationship> BindRelationship { get; set; }
    }

    internal class BindRelationship
    {
        public string ExchangeName { get; set; }
        public string QueueName { get; set; }
        public string RouteKey { get; set; }
    }
}
