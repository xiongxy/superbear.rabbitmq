using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace SuperBear.RabbitMq.Build
{
    public class Exchange
    {
        /// <summary>
        /// 交换器名称
        /// </summary>
        public string Name { get; set; }
        public string RetryName => $"{Name}@retry";
        public string DeadLetterName => $"{Name}@failed";
        /// <summary>
        /// 交换器类型
        /// </summary>
        public ExchangeTypeEnum Type { get; set; }
        /// <summary>
        /// 是否自动删除,默认Flase
        /// </summary>
        public bool AutoDelete { get; set; } = false;
        /// <summary>
        /// 是否打开持久化,默认True
        /// </summary>
        public bool Durable { get; set; } = true;
        public void ExchangeDeclare(Channel channel)
        {
            channel.CurrentChannel.ExchangeDeclare(Name, Type.ToString().ToLower(), durable: Durable, autoDelete: AutoDelete);
        }
    }

    public enum ExchangeTypeEnum
    {
        Direct = 1,
        Fanout = 2
    }
}
