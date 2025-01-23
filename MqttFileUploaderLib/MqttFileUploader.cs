using System.Text;
using System.Text.Json;

using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.Rpc;

namespace MqttFileExchanger
{
    public class MqttFileUploader : IMqttFileUploader
    {
        #region DI
        private readonly IMqttRpcClient _mqttRpcClient;
        #endregion

        #region private fields
        public string _rootTopic { get; set; } = "";
        #endregion

        #region Public Properties
        public int PartSize = 1048576;
        public TimeSpan Timeout = TimeSpan.FromSeconds(3);
        #endregion

        #region Events
        public event EventHandler<MqttUploadFileProgressEventArgs>? UploadFileProgress;
        #endregion

        #region .ctor
        public MqttFileUploader(IMqttClient mqttClient, string rootTopic)
        {
            _rootTopic = rootTopic;

            var rpcOptions = new MqttRpcClientOptionsBuilder()
                .WithTopicGenerationStrategy(new MqttRpcClientTopicGenerationWithParametersStrategy(_rootTopic))
                .Build();

            _mqttRpcClient = new MqttClientFactory().CreateMqttRpcClient(mqttClient, rpcOptions);
        }
        #endregion

        public async Task<bool> UploadFile(string filename)
        {
            if (await UploadHeaderFile(filename))
                return await UploadPartsFile(filename);
            return false;
        }

        private async Task<bool> UploadHeaderFile(string filename)
        {
            try
            {
                FileInfo fileInfo = new FileInfo(filename);
                var parts = (int)Math.Ceiling(fileInfo.Length / (double)PartSize);
                var header = new Dictionary<string, object> { { "filename", fileInfo.Name }, { "size", fileInfo.Length }, { "parts", parts } };
                var payload = JsonSerializer.Serialize(header, header.GetType());

                var result = await _mqttRpcClient.ExecuteAsync(Timeout, MqttFileExchangerTopics.UploadHeaderTopic, payload, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                if (result == null) return false;

                var response = Encoding.Default.GetString(result);
                if (string.IsNullOrEmpty(response) || response != "ok")
                    return false;
            }
            catch
            {
                return false;
            }
            return true;
        }
        private async Task<bool> UploadPartsFile(string filename)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(File.OpenRead(filename), Encoding.ASCII))
                {
                    var part = 0;
                    var totalParts = (int)Math.Ceiling(reader.BaseStream.Length / (double)PartSize);
                    while (reader.PeekChar() != -1)
                    {
                        var bytes = reader.ReadBytes(PartSize);
                        var partPayload = new Dictionary<string, object> { { "part", part }, { "bytes", bytes } };
                        var jsonPayload = JsonSerializer.Serialize(partPayload, partPayload.GetType());
                        try
                        {
                            var partResult = await _mqttRpcClient.ExecuteAsync(Timeout, MqttFileExchangerTopics.UploadPartsTopic, jsonPayload, MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                            if (partResult == null) return false;

                            var response = Encoding.Default.GetString(partResult);
                            if (string.IsNullOrEmpty(response) || response != "ok")
                                return false;
                            part++;
                            UploadFileProgress?.Invoke(this, new MqttUploadFileProgressEventArgs() { CurrentPart = part, TotalPart = totalParts });
                        }
                        catch (MqttCommunicationException)
                        {
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }
    }
}
