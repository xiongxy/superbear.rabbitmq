using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SuperBear.RabbitMq.Build;
using ExchangeType = RabbitMQ.Client.ExchangeType;
using PublicationAddress = RabbitMQ.Client.PublicationAddress;
using Queue = SuperBear.RabbitMq.Build.Queue;

namespace SuperBear.RabbitMq.Extensions
{
    public static class ChannelExensions
    {
        /// <summary>
        /// 设置预读消息数
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="count"></param>
        public static void SetPrefetch(this Channel channel, ushort count)
        {
            channel.CurrentChannel.BasicQos(0, count, false);
        }
        /// <summary>
        /// 定义交换器
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="exchange"></param>
        /// <returns></returns>
        public static Channel DefineExchange(this Channel channel, Exchange exchange)
        {
            channel.Exchange = exchange;
            return channel;
        }
        /// <summary>
        /// 定义队列
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        public static Channel DefineQueue(this Channel channel, Queue queue)
        {
            channel.Queue = queue;
            return channel;
        }
        /// <summary>
        /// 绑定
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="routingKey"></param>
        /// <returns></returns>
        public static Channel Bind(this Channel channel, string routingKey)
        {
            channel.RoutingKey = routingKey;
            return channel;
        }
        /// <summary>
        /// 提交
        /// </summary>
        /// <param name="channel"></param>
        public static void Commit(this Channel channel)
        {
            channel.Exchange.ExchangeDeclare(channel);
            channel.Queue.QueueDeclare(channel);
            channel.CurrentChannel.QueueBind(channel.Queue.Name, channel.Exchange.Name, channel.RoutingKey, null);
        }
        public static IBasicProperties CreateBasicProperties(this Channel channel, BasicProperties basicProperties)
        {
            var properties = channel.CurrentChannel.CreateBasicProperties();
            properties.Persistent = basicProperties.Persistent;
            if (basicProperties.Priority != 0)
            {
                properties.Priority = basicProperties.Priority;
            }
            if (basicProperties.GenerateMessageId)
            {
                properties.MessageId = Guid.NewGuid().ToString("N");
            }
            if (basicProperties.GenerateTimestamp)
            {
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
            return properties;
        }
        public static void Publish<T>(this Channel channel, IBasicProperties properties, T body, PublicationAddress address = null) where T : class
        {
            if (address == null)
            {
                var exchange = channel.Exchange;
                address =
                    new PublicationAddress(exchange.Type.ToString().ToLower(), exchange.Name, channel.RoutingKey);
            }
            var jsonb = JsonConvert.SerializeObject(body);
            var message = Encoding.UTF8.GetBytes(jsonb);
            channel.CurrentChannel.BasicPublish(address, properties, message);
        }
        public static void Publish(this Channel channel, IBasicProperties properties, byte[] body, PublicationAddress address = null)
        {
            if (address == null)
            {
                var exchange = channel.Exchange;
                address =
                    new PublicationAddress(exchange.Type.ToString().ToLower(), exchange.Name, channel.RoutingKey);
            }
            channel.CurrentChannel.BasicPublish(address, properties, body);
        }
        public static void Receive<T>(this Channel channel, EventHandler<BasicDeliverEventArgs> received, string queName = "default")
        {
            queName = queName == "default" ? channel.Queue.Name : queName;
            if (channel.Queue.Retry)
            {
                ReceiveRetryMode<T>(channel, received, queName);
            }
            else if (channel.Queue.DeadLetter)
            {
                ReceiveDeadLetterMode<T>(channel, received, queName);
            }
            else
            {
                ReceiveNormal<T>(channel, received, queName);
            }
        }
        private static void ReceiveNormal<T>(Channel channel, EventHandler<BasicDeliverEventArgs> received, string queName)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                string body = Encoding.UTF8.GetString(ea.Body);
                var message = JsonConvert.DeserializeObject<T>(body);
                try
                {
                    received(message, ea);
                    currentChannel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception)
                {
                    currentChannel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            currentChannel.BasicConsume(queName, false, consumer);
        }
        private static void ReceiveRetryMode<T>(Channel channel, EventHandler<BasicDeliverEventArgs> received, string queName)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                try
                {
                    string body = Encoding.UTF8.GetString(ea.Body);
                    var message = JsonConvert.DeserializeObject<T>(body);
                    received(message, ea);
                }
                catch (Exception)
                {
                    var properties = ea.BasicProperties;
                    long retryCount = GetRetryCount(properties);
                    if (retryCount > 3)
                    {
                        IDictionary<String, Object> headers = new Dictionary<String, Object>();
                        headers.Add("x-orig-routing-key", GetOrigRoutingKey(properties, ea.RoutingKey));
                        var address = new PublicationAddress(ExchangeType.Direct, channel.Exchange.DeadLetterName, channel.RoutingKey);
                        channel.Publish(CreateOverrideProperties(properties, headers), ea.Body, address);
                    }
                    else
                    {
                        IDictionary<String, Object> headers = properties.Headers;
                        if (headers == null)
                        {
                            headers = new Dictionary<String, Object>();
                            headers.Add("x-orig-routing-key", GetOrigRoutingKey(properties, ea.RoutingKey));
                        }
                        else
                        {
                            headers["x-orig-routing-key"] = GetOrigRoutingKey(properties, ea.RoutingKey);
                        }
                        var address = new PublicationAddress(ExchangeType.Direct, channel.Exchange.RetryName, channel.RoutingKey);
                        channel.Publish(CreateOverrideProperties(properties, headers), ea.Body, address);
                    }
                }
            };
            currentChannel.BasicConsume(queName, true, consumer);
        }
        private static void ReceiveDeadLetterMode<T>(Channel channel, EventHandler<BasicDeliverEventArgs> received, string queName)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                try
                {
                    string body = Encoding.UTF8.GetString(ea.Body);
                    var message = JsonConvert.DeserializeObject<T>(body);
                    received(message, ea);
                    currentChannel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception)
                {
                    currentChannel.BasicNack(ea.DeliveryTag, false, false);
                }
            };
            currentChannel.BasicConsume(queName, false, consumer);
        }
        private static long GetRetryCount(IBasicProperties properties)
        {
            var retryCount = 0L;
            try
            {
                IDictionary<string, object> headers = properties.Headers;
                if (headers != null)
                {
                    if (headers.ContainsKey("x-death"))
                    {
                        var xdeath = headers["x-death"];
                        var xdeathA = (IList<object>)xdeath;
                        if (xdeathA.Any())
                        {
                            var xdeathB = xdeathA.FirstOrDefault();
                            var xdeathC = (IDictionary<string, object>)xdeathB;
                            if (xdeathC != null) retryCount = (long)xdeathC["count"];
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return retryCount;
        }
        private static string GetOrigRoutingKey(IBasicProperties properties, string defaultValue)
        {
            var routingKey = defaultValue;
            try
            {
                IDictionary<string, object> headers = properties.Headers;
                if (headers != null)
                {
                    if (headers.ContainsKey("x-orig-routing-key"))
                    {
                        var routingKeyByte = (byte[])headers["x-orig-routing-key"];
                        routingKey = Encoding.UTF8.GetString(routingKeyByte);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return routingKey;
        }
        private static IBasicProperties CreateOverrideProperties(IBasicProperties properties, IDictionary<string, object> headers)
        {
            properties.Headers = headers;
            return properties;
        }
    }
}
