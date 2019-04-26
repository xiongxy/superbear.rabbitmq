using System;
using System.Collections.Generic;
using System.Text;
using SuperBear.RabbitMq.Build;
using RabbitMQ.Client;
using System.Linq;
namespace SuperBear.RabbitMq
{
    public class MessageStructure
    {
        public Exchange Exchange { get; set; }
        public Queue Queue { get; set; }
        public string RoutingKey { get; set; }
        public void Commit(Channel channel)
        {
            var success = MemoryMap.Add(this);
            if (success)
            {
                ExchangeDeclare(channel);
                QueueDeclare(channel);
                channel.CurrentChannel.QueueBind(Queue.Name, Exchange.Name, RoutingKey, null);
            }
        }
        private void ExchangeDeclare(Channel channel)
        {
            channel.CurrentChannel.ExchangeDeclare(Exchange.Name, Exchange.Type.ToString().ToLower(), durable: Exchange.Durable, autoDelete: Exchange.AutoDelete);
        }
        private void QueueDeclare(Channel channel)
        {
            IDictionary<string, object> argumentDictionary = new Dictionary<string, object>();
            if (Queue.Priority)
            {
                argumentDictionary.Add("x-max-priority", 10);
            }
            if (Queue.Retry)
            {
                SetRetry(channel, argumentDictionary);
                return;
            }
            if (Queue.DeadLetter && !Queue.Retry)
            {
                SetDeadLetter(channel, argumentDictionary);
                return;
            }
            channel.CurrentChannel.QueueDeclare(Queue.Name, durable: Queue.Durable, exclusive: Queue.Exclusive, autoDelete: Queue.AutoDelete, arguments: !argumentDictionary.Any() ? null : argumentDictionary);
        }
        private void SetRetry(Channel channel, IDictionary<string, object> argumentDictionary)
        {
            #region 错误队列

            var deadLetter = new DeadLetter()
            {
                Exchange = $"{Exchange.Name}@failed",
                RoutingKey = $"{RoutingKey}",
                Queue = $"{Queue.Name}@failed"
            };
            channel.CurrentChannel.ExchangeDeclare(deadLetter.Exchange, ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare(deadLetter.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.CurrentChannel.QueueBind(deadLetter.Queue, deadLetter.Exchange, deadLetter.RoutingKey, null);
            #endregion

            #region 重试队列

            IDictionary<string, object> retryArgumentDictionary = new Dictionary<string, object>();
            retryArgumentDictionary.Add("x-dead-letter-exchange", Exchange.Name);
            retryArgumentDictionary.Add("x-dead-letter-routing-key", RoutingKey);
            retryArgumentDictionary.Add("x-message-ttl", 30 * 1000);

            channel.CurrentChannel.ExchangeDeclare($"{Exchange.Name}@retry", ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare($"{Queue.Name}@retry", durable: true, exclusive: false, autoDelete: false, arguments: retryArgumentDictionary);
            channel.CurrentChannel.QueueBind($"{Queue.Name}@retry", $"{Exchange.Name}@retry", deadLetter.RoutingKey, null);
            #endregion

            #region 主队列
            channel.CurrentChannel.QueueDeclare(Queue.Name, durable: Queue.Durable, exclusive: Queue.Exclusive, autoDelete: Queue.AutoDelete, arguments: !argumentDictionary.Any() ? null : argumentDictionary);
            #endregion
        }
        private void SetDeadLetter(Channel channel, IDictionary<string, object> argumentDictionary)
        {

            var deadLetterConfig = new DeadLetter()
            {
                Exchange = $"{Exchange.Name}@failed",
                RoutingKey = $"{RoutingKey}",
                Queue = $"{Queue.Name}@failed"
            };
            channel.CurrentChannel.ExchangeDeclare(deadLetterConfig.Exchange, ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare(deadLetterConfig.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.CurrentChannel.QueueBind(deadLetterConfig.Queue, deadLetterConfig.Exchange, deadLetterConfig.RoutingKey, null);
            argumentDictionary.Add("x-dead-letter-exchange", deadLetterConfig.Exchange);
            argumentDictionary.Add("x-dead-letter-routing-key", deadLetterConfig.RoutingKey);
            channel.CurrentChannel.QueueDeclare(Queue.Name, durable: Queue.Durable, exclusive: Queue.Exclusive, autoDelete: Queue.AutoDelete, arguments: argumentDictionary);
        }
    }
}
