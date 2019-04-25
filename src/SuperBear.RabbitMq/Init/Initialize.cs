using System;
using SuperBear.RabbitMq.Build;

namespace SuperBear.RabbitMq.Init
{
    public class Initialize
    {
        public static void Init(Action<Initializer> config, Factory factory)
        {
            config(new Initializer(factory));
        }
    }
}
