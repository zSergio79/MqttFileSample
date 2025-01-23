
namespace MqttFileExchanger
{
    public interface IMqttFileDownloader
    {
        TimeSpan DownloadTimeout { get; set; }
        string Folder { get; set; }

        event EventHandler<string>? DownloadCancelled;
        event EventHandler<bool>? DownloadCompleted;
        event EventHandler<MqttUploadFileProgressEventArgs>? DownloadProgress;
        event EventHandler<string>? DownloadStarted;

        Task Listen();
    }
}