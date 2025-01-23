using MQTTnet;

namespace MqttFileExchanger
{
    public class MqttFileDownloaderBuilder
    {
        #region private fields
        private IMqttClient _mqttClient;
        private string _rootTopic = "";
        private TimeSpan _timeout = TimeSpan.FromSeconds(3);
        private string _folder = "";
        #endregion

        #region .ctor
        public MqttFileDownloaderBuilder(IMqttClient mqttClient)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
        }
        #endregion

        #region With Methods
        public MqttFileDownloaderBuilder WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return this;
        }

        public MqttFileDownloaderBuilder WithTimeout(int milliseconds)
        {
            _timeout = TimeSpan.FromMilliseconds(milliseconds);
            return this;
        }

        public MqttFileDownloaderBuilder WithFolder(string folder)
        {
            _folder = folder;
            return this;
        }

        public MqttFileDownloaderBuilder WithRootTopic(string rootTopic)
        {
            _rootTopic = rootTopic;
            return this;
        }
        #endregion

        #region Build
        public MqttFileDownloader Build()
        {
            return new MqttFileDownloader(_mqttClient, _rootTopic)
            {
                DownloadTimeout = _timeout,
                Folder = _folder
            };
        }
        #endregion
    }
}
