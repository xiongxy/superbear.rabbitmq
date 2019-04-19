using System;
using System.Collections.Generic;
using System.Text;

namespace SuperBear.RabbitMq
{
    public class RabbitOption
    {
        public string UserName { get; set; } = "guest";
        public string Password { get; set; } = "guest";
        public string HostName { get; set; }
        public string Port { get; set; }
        public string EnvironmentName { get; set; }
        public RabbitMqAdditionalConfig AdditionalConfig { get; set; } = new RabbitMqAdditionalConfig();
    }

    public class RabbitMqAdditionalConfig
    {
        public bool AutomaticRecoveryEnabled { get; set; } = false;
    }
}
