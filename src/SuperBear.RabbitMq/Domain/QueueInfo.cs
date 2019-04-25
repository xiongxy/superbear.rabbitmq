using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SuperBear.RabbitMq.Domain
{
    public class QueuesInfo
    {
        [JsonProperty("item_count")]
        public int QueueCount { get; set; }
        [JsonProperty("items")]
        public List<QueueInfo> Queues { get; set; }
    }
    public class QueueInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("state")]
        public string State { get; set; }
        [JsonProperty("messages_ready")]
        public int ReadyMessage { get; set; }
        [JsonProperty("messages_unacknowledged")]
        public int UnackedMessage { get; set; }
        [JsonProperty("messages")]
        public int TotalMessage { get; set; }
    }
}
