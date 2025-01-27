using MQTTnet;
using MQTTnet.Diagnostics.PacketInspection;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReconnectSubscriberMqtt.Model
{
    public class ReconnectableMqttClient : IMqttClient
    {
        #region private fields
        private readonly IMqttClient _mqttClient;
        private bool _isManual = false;
        private int _tryNumber = 0;
        #endregion

        #region Public Properties
        public IMqttReconnectTimePolicy ReconnectTimePolicy { get; set; } = new DefaultMqttReconnectTimePolicy();
        #endregion

        #region .ctor
        public ReconnectableMqttClient(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient;

            #region Events
            _mqttClient.ApplicationMessageReceivedAsync += (args) =>
            {
                if(ApplicationMessageReceivedAsync == null) return Task.CompletedTask;
                return ApplicationMessageReceivedAsync?.Invoke(args);
            };
            _mqttClient.ConnectedAsync += (args) =>
            {
                _tryNumber = 0;
                if(ConnectedAsync == null) return Task.CompletedTask;
                return ConnectedAsync?.Invoke(args);
            };
            _mqttClient.ConnectingAsync += (args) => 
            {
                if (ConnectingAsync == null) return Task.CompletedTask;
                return ConnectingAsync?.Invoke(args); 
            };
            //TODO Проверить с остановкой сервиса Mosquitto
            _mqttClient.DisconnectedAsync += async (args) => 
            {
                if (DisconnectedAsync == null) return;
                await DisconnectedAsync.Invoke(args);

                if (args.ClientWasConnected && !_isManual)
                {
                    _tryNumber++;
                    var delay = ReconnectTimePolicy.GetDelay(_tryNumber);
                    if (delay > 0)
                        await Task.Delay(delay);
                    await _mqttClient.ConnectAsync(Options);
                }
                
                return;
            };
            _mqttClient.InspectPacketAsync += (args) => 
            {
                if (InspectPacketAsync == null) return Task.CompletedTask;
                return InspectPacketAsync?.Invoke(args); 
            };
            #endregion
        }
        #endregion

        #region Implentation

        #region Properties
        public bool IsConnected { get => _mqttClient.IsConnected; }
        public MqttClientOptions Options { get => _mqttClient.Options; }
        #endregion

        #region Events
        public event Func<MqttApplicationMessageReceivedEventArgs, Task>? ApplicationMessageReceivedAsync;
        public event Func<MqttClientConnectedEventArgs, Task>? ConnectedAsync;
        public event Func<MqttClientConnectingEventArgs, Task>? ConnectingAsync;
        public event Func<MqttClientDisconnectedEventArgs, Task>? DisconnectedAsync;
        public event Func<InspectMqttPacketEventArgs, Task>? InspectPacketAsync;
        #endregion

        #region Methods
        public Task<MqttClientConnectResult> ConnectAsync(MqttClientOptions options, CancellationToken cancellationToken = default)
        {
            _isManual = false;
            return _mqttClient.ConnectAsync(options, cancellationToken);
        }
        public Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken = default)
        {
            _isManual = true;
            return _mqttClient.DisconnectAsync(options, cancellationToken);
        }
        public void Dispose() => 
            _mqttClient.Dispose();
        public Task PingAsync(CancellationToken cancellationToken = default) => 
            _mqttClient.PingAsync(cancellationToken);
        public Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken = default) => 
            _mqttClient.PublishAsync(applicationMessage, cancellationToken);
        public Task SendEnhancedAuthenticationExchangeDataAsync(MqttEnhancedAuthenticationExchangeData data, CancellationToken cancellationToken = default) => 
            _mqttClient.SendEnhancedAuthenticationExchangeDataAsync(data, cancellationToken);
        public Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken = default) =>
            _mqttClient.SubscribeAsync(options, cancellationToken);
        public Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken = default) => 
            _mqttClient.UnsubscribeAsync(options, cancellationToken);
        #endregion

        #endregion


    }
}
