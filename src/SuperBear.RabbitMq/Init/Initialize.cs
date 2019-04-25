using System;
using RabbitMQ.Client;
using SuperBear.RabbitMq.Build;

namespace SuperBear.RabbitMq.Init
{
    public sealed class Initialize
    {
        private Initialize()
        {

        }
        public static void Init(Action<Initialize> config)
        {
            config(new Initialize());
        }
        public void InitMessageStructure(MessageStructure messageStructure)
        {
            MemoryMap.MessageStructures.Add(messageStructure);
        }
        internal static void Init(Channel channel)
        {
            foreach (var messageStructure in MemoryMap.MessageStructures)
            {
                messageStructure.Commit(channel);
            }
        }
    }
}
