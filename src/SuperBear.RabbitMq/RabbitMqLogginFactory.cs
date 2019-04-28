using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;

namespace SuperBear.RabbitMq
{
    public class RabbitMqLogginFactory
    {
        private readonly ILoggerFactory _loggerFactory;
        public RabbitMqLogginFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }
        public ILogger CreateLogger()
        {
            return _loggerFactory.CreateLogger("SuperBear.RabbitMq");
        }
    }
}
