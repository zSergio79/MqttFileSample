
namespace MqttFileExchanger
{
    public interface IMqttFileUploader
    {
        string _rootTopic { get; set; }

        event EventHandler<MqttUploadFileProgressEventArgs>? UploadFileProgress;

        Task<bool> UploadFile(string filename);
    }
}