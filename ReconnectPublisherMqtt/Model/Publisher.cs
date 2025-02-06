using MQTTnet;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReconnectPublisherMqtt.Model
{
    public class Publisher : IDisposable
    {
        #region Mqtt fields
        private readonly IMqttClient _mqttClient;
        #endregion

        #region private fields
        private int _interval = 1000;
        private long _currentTick;
        private Timer _publishTimer;
        #endregion

        #region Public Properties
        public string StaticMessage { get; set; } = "static message";
        public Func<long, string>? GetMessage { get; set; } = null;
        public string Topic { get; set; } = "reconTopic";
        #endregion

        #region ctor
        public Publisher(IMqttClient client)
        {
            _publishTimer = new Timer(PublishTimerTick);
            _mqttClient = client ?? throw new ArgumentNullException(nameof(client)); 
        }
        #endregion

        #region Control
        public void Start()
        {
            _currentTick = 0;
            _publishTimer.Change(_interval, _interval);
        }
        public void Stop()
        {
            _publishTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        #endregion

        #region Tick
        private async void PublishTimerTick(object? state)
        {
            try
            {
                if (_mqttClient == null || !_mqttClient.IsConnected)
                    return;

                var messageBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic(Topic)
                    .WithRetainFlag(true);

                var payload = StaticMessage;
                if (GetMessage != null)
                    payload = GetMessage(_currentTick);

                var message = messageBuilder
                    .WithPayload(payload)
                    .Build();

                await _mqttClient.PublishAsync(message);
            }
            catch
            { }
            finally
            {
                _currentTick++;
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_publishTimer != null) _publishTimer.Dispose();
            }
        }
        #endregion
    }
}
