using MQTTnet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReconnectSubscriberMqtt.Model
{
    public class Subscriber
    {
        #region DI
        private readonly IMqttClient _mqttClient;
        #endregion

        #region Public Properties
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1883;
        #endregion

        #region private fields
        private string _topic { get; set; } = "reconTopic";
        #endregion

        #region Event
        public event EventHandler<string>? MessageReceived;
        #endregion

        #region .ctor
        public Subscriber(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient;
        }
        #endregion

        #region Methods
        public async Task<bool> Connect()
        {
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(Host, Port)
                    .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
                    .WithCleanStart()
                    .Build();
                var result = (await _mqttClient.ConnectAsync(options)).ResultCode == MqttClientConnectResultCode.Success;
                return result;
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        public async Task Disconnect()
        {
            try
            {
                if(_mqttClient.IsConnected)
                    await _mqttClient.DisconnectAsync();
            }
            catch
            { }
        }
        public async Task StartListen(string topic)
        {
            //await StopListen();
            if(string.IsNullOrEmpty(topic))
                throw new ArgumentNullException(nameof(topic));

            _topic = topic;

            var options = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(_topic)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessage;

            await _mqttClient.SubscribeAsync(options);
        }

        public async Task StopListen() 
        {
            var options = new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter(_topic)
                .Build();
            _mqttClient.ApplicationMessageReceivedAsync -= OnMessage;
            await _mqttClient.UnsubscribeAsync(_topic);
        }
        #endregion

        #region Messaging
        private async Task OnMessage(MqttApplicationMessageReceivedEventArgs arg)
        {
            if(arg.ApplicationMessage.Topic == _topic)
            {
                var payload = arg.ApplicationMessage.ConvertPayloadToString();
                MessageReceived?.Invoke(this, payload);
            }
        }
        #endregion
    }
}
