using MQTTnet;

using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MqttFileExchanger
{
    public class MqttFileDownloader : IMqttFileDownloader
    {
        #region DI
        private readonly IMqttClient _mqttClient;
        private readonly string _rootTopic;
        #endregion

        #region Events
        public event EventHandler<string>? DownloadStarted;
        public event EventHandler<string>? DownloadCancelled;
        public event EventHandler<bool>? DownloadCompleted;
        public event EventHandler<MqttUploadFileProgressEventArgs>? DownloadProgress;
        #endregion

        #region private properties/fields
        private string _headerTopic => $"{_rootTopic}/{MqttFileExchangerTopics.UploadHeaderTopic}";
        private string _partTopic => $"{_rootTopic}/{MqttFileExchangerTopics.UploadPartsTopic}";
        private Timer _timeoutTimer;
        #endregion

        #region private fields
        private string? _filename;
        private int _fileSize;
        private int _totalParts;
        private int _currentPart;
        private bool _inprocess;
        private BinaryWriter? _writer;
        #endregion

        #region Public Properties
        public string Folder { get; set; } = "";
        public TimeSpan DownloadTimeout { get; set; } = TimeSpan.FromSeconds(3);
        #endregion

        #region .ctor
        public MqttFileDownloader(IMqttClient mqttClient, string rootTopic)
        {
            _mqttClient = mqttClient ?? throw new ArgumentNullException(nameof(mqttClient));
            _rootTopic = rootTopic ?? throw new ArgumentNullException(nameof(rootTopic));
            _timeoutTimer = new Timer(DownloadWatchDog);
        }


        #endregion

        #region Methods
        public async Task Listen()
        {
            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(_headerTopic)
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += HeaderListener;
            await _mqttClient.SubscribeAsync(subscribeOptions);
        }

        private async Task HeaderListener(MqttApplicationMessageReceivedEventArgs arg)
        {
            var message = arg.ApplicationMessage;
            if (message.Topic == _headerTopic)
            {
                if (_inprocess)
                    return;
                //throw new InvalidOperationException("Download already start");

                var json = JsonSerializer.Deserialize<JsonElement>(Encoding.Default.GetString(message.Payload));
                _fileSize = json.GetProperty("size").GetInt32();
                _filename = json.GetProperty("filename").GetString();
                _totalParts = json.GetProperty("parts").GetInt32();

                var responseBuilder = new MqttApplicationMessageBuilder()
                    .WithTopic($"{message.Topic}/response");

                if (!string.IsNullOrEmpty(_filename) && _fileSize > 0 && _totalParts > 0)
                {
                    responseBuilder.WithPayload("ok");
                    _mqttClient.ApplicationMessageReceivedAsync += PartsListener;
                    DownloadStarted?.Invoke(this, _filename);

                    _filename = Path.Combine(Folder, _filename);
                    _inprocess = true;
                    _currentPart = 0;
                    _timeoutTimer.Change((int)DownloadTimeout.TotalMilliseconds, Timeout.Infinite);

                    if (File.Exists(_filename)) File.Delete(_filename);
                    _writer = new BinaryWriter(File.OpenWrite(_filename));

                    var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(_partTopic)
                        .Build();
                    await _mqttClient.SubscribeAsync(subscribeOptions);
                }
                else
                {
                    DownloadCancelled?.Invoke(this, _filename ?? "<undefined file name>");
                    responseBuilder.WithPayload("fail");
                    _filename = string.Empty;
                    _totalParts = 0;
                }
                await _mqttClient.PublishAsync(responseBuilder.Build());
            }
        }

        private async Task PartsListener(MqttApplicationMessageReceivedEventArgs arg)
        {
            var message = arg.ApplicationMessage;
            if (message.Topic == _partTopic)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(Encoding.Default.GetString(arg.ApplicationMessage.Payload));
                var part = json.GetProperty("part").GetInt32();
                var bytes = json.GetProperty("bytes").GetBytesFromBase64();

                var response = new MqttApplicationMessageBuilder()
                    .WithTopic($"{_partTopic}/response")
                    .WithPayload("ok")
                    .Build();

                if (_currentPart != part || _writer == null)
                {
                    Debug.WriteLine($"download fail part#{_currentPart}");
                    DownloadCompleted?.Invoke(this, false);
                    return;
                }

                _writer?.Write(bytes);
                _currentPart = part + 1;
                DownloadProgress?.Invoke(this, new MqttUploadFileProgressEventArgs() { CurrentPart = _currentPart, TotalPart = _totalParts });
                _timeoutTimer.Change((int)DownloadTimeout.TotalMilliseconds, Timeout.Infinite);

                await _mqttClient.PublishAsync(response);

                if (_currentPart == _totalParts)
                    Close(true);
            }
        }

        private void Close(bool result)
        {
            _mqttClient.ApplicationMessageReceivedAsync -= PartsListener;
            try
            {
                _inprocess = false;
                _writer?.Flush();
                _writer?.Close();
                _timeoutTimer.Change(Timeout.Infinite, Timeout.Infinite);
                if (result)
                    DownloadCompleted?.Invoke(this, true);
                else
                {
                    DownloadCancelled?.Invoke(this, "Timeout");
                    if (File.Exists(_filename)) File.Delete(_filename);
                }
            }
            finally
            {
                _writer?.Dispose();
                _writer = null;
            }
        }

        private void DownloadWatchDog(object? state)
        {
            if (_inprocess)
                Close(false);
        }
        #endregion
    }
}
