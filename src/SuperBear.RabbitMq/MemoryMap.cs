using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SuperBear.RabbitMq.Build;

namespace SuperBear.RabbitMq
{
    internal static class MemoryMap
    {
        static MemoryMap()
        {
            MessageStructures = new ConcurrentBag<MessageStructure>();
        }
        internal static System.Collections.Concurrent.ConcurrentBag<MessageStructure> MessageStructures { get; set; }
        public static MessageStructure GetMessageStructure(string exchangeName = null, string queueName = null, string routingKey = null)
        {
            var messageStructures = MessageStructures.AsQueryable();
            if (!string.IsNullOrEmpty(exchangeName))
            {
                messageStructures = messageStructures.Where(x => x.Exchange.Name == exchangeName);
            }
            if (!string.IsNullOrEmpty(queueName))
            {
                messageStructures = messageStructures.Where(x => x.Queue.Name == queueName);
            }
            if (!string.IsNullOrEmpty(routingKey))
            {
                messageStructures = messageStructures.Where(x => x.RoutingKey == routingKey);
            }
            return messageStructures.FirstOrDefault();
        }
        public static bool Add(MessageStructure messageStructure)
        {
            if (MessageStructures.All(x => x.Queue.Name != messageStructure.Queue.Name))
            {
                MessageStructures.Add(messageStructure);
            }
            return true;
        }
    }

}
