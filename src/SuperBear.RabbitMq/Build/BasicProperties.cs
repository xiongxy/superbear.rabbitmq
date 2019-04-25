using System;
using System.Collections.Generic;
using System.Text;

namespace SuperBear.RabbitMq.Build
{
    public class BasicProperties
    {
        /// <summary>
        /// 是否开启消息持久化,默认True
        /// </summary>
        public bool Persistent { get; set; } = true;
        /// <summary>
        /// 是否开启生成消息Id,默认False
        /// </summary>
        public bool GenerateMessageId { get; set; } = false;
        /// <summary>
        /// 是否开启时间戳,默认False
        /// </summary>
        public bool GenerateTimestamp { get; set; } = false;

        public byte Priority { get; set; } = 0;
    }
}
