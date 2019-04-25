using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RabbitMQ.Client;

namespace SuperBear.RabbitMq.Build
{
    public class Queue
    {
        /// <summary>
        /// 队列名称
        /// </summary>
        public string Name { get; set; }
        public string RetryName => $"{Name}@retry";
        public string DeadLetterName => $"{Name}@failed";
        /// <summary>
        /// 是否开启持久化,默认True
        /// </summary>
        public bool Durable { get; set; } = true;
        /// <summary>
        /// 是否开启排他性,默认False
        /// </summary>
        public bool Exclusive { get; set; } = false;
        /// <summary>
        /// 是否自动删除,默认False
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 是否开启重试机制,默认Flase
        /// </summary>
        public bool Retry { get; set; } = false;
        /// <summary>
        /// 是否开启死信,默认Flase
        /// </summary>
        public bool DeadLetter { get; set; } = false;
        /// <summary>
        /// 是否开启优先级,默认False
        /// </summary>
        public bool Priority { get; set; } = false;
        private DeadLetter DeadLetterConfig { get; set; }

        public Queue(string name)
        {
            Name = name;
        }

        public void QueueDeclare(Channel channel)
        {
            IDictionary<string, object> argumentDictionary = new Dictionary<string, object>();
            if (Priority)
            {
                argumentDictionary.Add("x-max-priority", 10);
            }
            if (Retry)
            {
                SetRetry(channel, argumentDictionary);
                return;
            }
            if (DeadLetter && !Retry)
            {
                SetDeadLetter(channel, argumentDictionary);
                return;
            }
            channel.CurrentChannel.QueueDeclare(Name, durable: Durable, exclusive: Exclusive, autoDelete: AutoDelete, arguments: !argumentDictionary.Any() ? null : argumentDictionary);
        }
        private void SetRetry(Channel channel, IDictionary<string, object> argumentDictionary)
        {
            #region 错误队列
            if (DeadLetterConfig == null)
            {
                DeadLetterConfig = new DeadLetter()
                {
                    Exchange = $"{channel.Exchange.Name}@failed",
                    RoutingKey = $"{channel.RoutingKey}",
                    Queue = $"{channel.Queue.Name}@failed"
                };
            }
            channel.CurrentChannel.ExchangeDeclare(DeadLetterConfig.Exchange, ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare(DeadLetterConfig.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.CurrentChannel.QueueBind(DeadLetterConfig.Queue, DeadLetterConfig.Exchange, DeadLetterConfig.RoutingKey, null);
            #endregion

            #region 重试队列

            IDictionary<string, object> retryArgumentDictionary = new Dictionary<string, object>();
            retryArgumentDictionary.Add("x-dead-letter-exchange", channel.Exchange.Name);
            retryArgumentDictionary.Add("x-dead-letter-routing-key", channel.RoutingKey);
            retryArgumentDictionary.Add("x-message-ttl", 30 * 1000);

            channel.CurrentChannel.ExchangeDeclare($"{channel.Exchange.Name}@retry", ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare($"{channel.Queue.Name}@retry", durable: true, exclusive: false, autoDelete: false, arguments: retryArgumentDictionary);
            channel.CurrentChannel.QueueBind($"{channel.Queue.Name}@retry", $"{channel.Exchange.Name}@retry", DeadLetterConfig.RoutingKey, null);
            #endregion

            #region 主队列
            channel.CurrentChannel.QueueDeclare(Name, durable: Durable, exclusive: Exclusive, autoDelete: AutoDelete, arguments: !argumentDictionary.Any() ? null : argumentDictionary);
            #endregion
        }
        private void SetDeadLetter(Channel channel, IDictionary<string, object> argumentDictionary)
        {
            if (DeadLetterConfig == null)
            {
                DeadLetterConfig = new DeadLetter()
                {
                    Exchange = $"{channel.Exchange.Name}@failed",
                    RoutingKey = $"{channel.RoutingKey}",
                    Queue = $"{channel.Queue.Name}@failed"
                };
            }
            channel.CurrentChannel.ExchangeDeclare(DeadLetterConfig.Exchange, ExchangeTypeEnum.Direct.ToString().ToLower(), durable: true, autoDelete: false);
            channel.CurrentChannel.QueueDeclare(DeadLetterConfig.Queue, durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.CurrentChannel.QueueBind(DeadLetterConfig.Queue, DeadLetterConfig.Exchange, DeadLetterConfig.RoutingKey, null);
            argumentDictionary.Add("x-dead-letter-exchange", DeadLetterConfig.Exchange);
            argumentDictionary.Add("x-dead-letter-routing-key", DeadLetterConfig.RoutingKey);
            channel.CurrentChannel.QueueDeclare(Name, durable: Durable, exclusive: Exclusive, autoDelete: AutoDelete, arguments: argumentDictionary);
        }
    }
    public class DeadLetter
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }
    }
}
