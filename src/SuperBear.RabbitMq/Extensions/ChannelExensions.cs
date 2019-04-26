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
        /// 创建BasicProperties
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="basicProperties"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel"></param>
        /// <param name="properties"></param>
        /// <param name="body"></param>
        /// <param name="address"></param>
        public static void Publish(this Channel channel, IBasicProperties properties, byte[] body, PublicationAddress address)
        {
            channel.CurrentChannel.BasicPublish(address, properties, body);
        }
        /// <summary>
        /// 发布消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="properties"></param>
        /// <param name="body"></param>
        /// <param name="address"></param>
        public static void Publish<T>(this Channel channel, IBasicProperties properties, T body, PublicationAddress address) where T : class
        {
            var jsonb = JsonConvert.SerializeObject(body);
            var message = Encoding.UTF8.GetBytes(jsonb);
            channel.CurrentChannel.BasicPublish(address, properties, message);
        }
        /// <summary>
        /// 接收消息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="channel"></param>
        /// <param name="received"></param>
        /// <param name="queueName"></param>
        public static void Receive<T>(this Channel channel, Action<T, BasicDeliverEventArgs> received, string queueName)
        {
            var messageStructure = MemoryMap.GetMessageStructure(queueName: queueName);
            if (messageStructure == null)
            {
                throw new Exception("队列不在映射中!");
            }
            var queue = messageStructure.Queue;
            if (queue.Retry)
            {
                ReceiveRetryMode<T>(channel, received, messageStructure);
            }
            else if (queue.DeadLetter)
            {
                ReceiveDeadLetterMode<T>(channel, received, messageStructure);
            }
            else
            {
                ReceiveNormal<T>(channel, received, messageStructure);
            }
        }
        private static void ReceiveNormal<T>(Channel channel, Action<T, BasicDeliverEventArgs> received, MessageStructure messageStructure)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body);
                try
                {
                    var message = JsonConvert.DeserializeObject<T>(body);
                    try
                    {
                        received(message, ea);
                    }
                    catch (Exception e)
                    {
                        channel.Logger.LogError($"{messageStructure.Queue.Name} Queue 执行失败,原因:{{0}},消息体{{1}}", e, body);
                        currentChannel.BasicNack(ea.DeliveryTag, false, true);
                    }
                }
                catch (Exception)
                {
                    channel.Logger.LogError($"Json解析{typeof(T).Name}失败:{{0}}", body);
                    currentChannel.BasicNack(ea.DeliveryTag, false, true);
                }
            };
            currentChannel.BasicConsume(messageStructure.Queue.Name, false, consumer);
        }
        private static void ReceiveRetryMode<T>(Channel channel, Action<T, BasicDeliverEventArgs> received, MessageStructure messageStructure)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body);
                try
                {
                    var message = JsonConvert.DeserializeObject<T>(body);
                    try
                    {
                        received(message, ea);
                    }
                    catch (Exception e)
                    {
                        var properties = ea.BasicProperties;
                        long retryCount = GetRetryCount(properties);
                        if (retryCount > 3)
                        {
                            IDictionary<String, Object> headers = new Dictionary<String, Object>();
                            headers.Add("x-orig-routing-key", GetOrigRoutingKey(properties, ea.RoutingKey));
                            var address = new PublicationAddress(ExchangeType.Direct, messageStructure.Exchange.DeadLetterName, messageStructure.RoutingKey);
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
                            var address = new PublicationAddress(ExchangeType.Direct, messageStructure.Exchange.RetryName, messageStructure.RoutingKey);
                            channel.Publish(CreateOverrideProperties(properties, headers), ea.Body, address);
                        }
                    }
                }
                catch (Exception)
                {
                    channel.Logger.LogError($"Json解析{typeof(T).Name}失败:{{0}}", body);
                    var properties = ea.BasicProperties;
                    var address = new PublicationAddress(ExchangeType.Direct, messageStructure.Exchange.DeadLetterName, messageStructure.RoutingKey);
                    channel.Publish(properties, ea.Body, address);
                }
            };
            currentChannel.BasicConsume(messageStructure.Queue.Name, true, consumer);
        }
        private static void ReceiveDeadLetterMode<T>(Channel channel, Action<T, BasicDeliverEventArgs> received, MessageStructure messageStructure)
        {
            var currentChannel = channel.CurrentChannel;
            EventingBasicConsumer consumer = new EventingBasicConsumer(currentChannel);
            consumer.Received += (ch, ea) =>
            {
                var body = Encoding.UTF8.GetString(ea.Body);
                try
                {
                    var message = JsonConvert.DeserializeObject<T>(body);
                    try
                    {
                        received(message, ea);
                    }
                    catch (Exception e)
                    {
                        channel.Logger.LogError($"{messageStructure.Queue.Name} Queue 执行失败,原因:{{0}},消息体{{1}}", e, body);
                        currentChannel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception)
                {
                    channel.Logger.LogError($"Json解析{typeof(T).Name}失败:{{0}}", body);
                    currentChannel.BasicNack(ea.DeliveryTag, false, false);
                }
            };
            currentChannel.BasicConsume(messageStructure.Queue.Name, false, consumer);
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
