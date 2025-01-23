using MQTTnet;

namespace MqttFileExchanger
{
    public class MqttFileUploaderBuilder
    {
        #region private fields
        private IMqttClient _mqttClient;
        private string _rootTopic = "";
        private int _partSize = 1048576;
        private TimeSpan _timeout = TimeSpan.FromSeconds(3);
        #endregion

        #region .ctor
        public MqttFileUploaderBuilder(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
        }
        #endregion

        #region With Methods
        public MqttFileUploaderBuilder WithRootTopic(string rootTopic)
        {
            _rootTopic = rootTopic;
            return this;
        }

        public MqttFileUploaderBuilder WithPartSize(int partSize)
        {
            if(partSize < 0) throw new ArgumentOutOfRangeException(nameof(partSize));
            _partSize = partSize;
            return this;
        }

        public MqttFileUploaderBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public MqttFileUploaderBuilder WithTimeout(int milliseconds)
        {
            if (milliseconds < 0) throw new ArgumentOutOfRangeException(nameof(milliseconds));
            _timeout = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }
        #endregion

        #region Build
        public MqttFileUploader Build() 
        {
            return new MqttFileUploader(_mqttClient, _rootTopic) 
            {
                PartSize = _partSize,
                Timeout = _timeout,
            };
        }
        #endregion
    }
}
