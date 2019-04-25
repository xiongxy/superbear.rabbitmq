using System;
using System.Collections.Generic;
using System.Text;
using SuperBear.RabbitMq.Build;
using SuperBear.RabbitMq.Extensions;

namespace SuperBear.RabbitMq.Init
{
    public class Initializer
    {
        private readonly Factory _factory;
        public Initializer(Factory factory)
        {
            _factory = factory;
        }
        public void InitQueue(Queue queue)
        {
            MemoryMap.Queues.TryAdd(queue.Name, queue);
            queue.QueueDeclare(_factory.CurrentConnection.CreateChannel());
        }
        public void InitExchange(Exchange exchange)
        {
            MemoryMap.Exchanges.TryAdd(exchange.Name, exchange);
            exchange.ExchangeDeclare(_factory.CurrentConnection.CreateChannel());
        }
        public void InitBind(Exchange exchange, Queue queue, string routeKey)
        {
            MemoryMap.BindRelationship.Add(new BindRelationship()
            {
                ExchangeName = exchange.Name,
                QueueName = exchange.Name,
                RouteKey = routeKey
            });
            _factory.CurrentConnection.CreateModel().QueueBind(queue.Name, exchange.Name, routeKey, null);
        }
    }
}
