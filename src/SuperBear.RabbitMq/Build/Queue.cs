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
        public Queue()
        {
            Name = Guid.NewGuid().ToString("N");
        }
        public Queue(string name)
        {
            Name = name;
        }

    }
    public class DeadLetter
    {
        public string Exchange { get; set; }
        public string Queue { get; set; }
        public string RoutingKey { get; set; }
    }
}
