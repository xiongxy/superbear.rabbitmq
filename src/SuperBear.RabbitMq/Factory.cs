using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SuperBear.RabbitMq
{
    public class Factory
    {
        private readonly RabbitOption _rabbitOption;
        private ConnectionFactory _instance;
        public ConnectionFactory Instance
        {
            get
            {
                if (_instance == null)
                {
                    var portStr = _rabbitOption.Port;
                    int.TryParse(portStr, out int portInt);
                    _instance = new ConnectionFactory
                    {
                        UserName = _rabbitOption.UserName,
                        Password = _rabbitOption.Password,
                        HostName = _rabbitOption.HostName,
                        AutomaticRecoveryEnabled = _rabbitOption.AdditionalConfig.AutomaticRecoveryEnabled
                    };
                    if (portInt != 0)
                    {
                        Instance.Port = portInt;
                    }
                }
                return _instance;
            }
        }
        private IConnection _currentConnection;
        public IConnection CurrentConnection
        {
            get
            {
                if (_currentConnection == null)
                {
                    _currentConnection = Instance.CreateConnection();
                }
                return _currentConnection;
            }
        }
        public Factory(IOptions<RabbitOption> rabbitOption)
        {
            _rabbitOption = rabbitOption.Value;
        }
    }
}
