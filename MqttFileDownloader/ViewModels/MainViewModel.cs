using MqttFileExchanger;

using MQTTnet;

using ReactiveUI.SourceGenerators;

using System;
using System.Collections.ObjectModel;


namespace FileDownloadWithMqtt.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region Mqtt
    private IMqttClient _mqttClient;
    private IMqttFileDownloader _mqttFileDownloader;
    #endregion

    #region Const
    private const string RootTopic = "fileExchanger";
    #endregion

    #region Bindable Properties
    [Reactive] private string _connectStatus;
    [Reactive] private string _message;
    [Reactive] private ObservableCollection<string> _messages;
    #region Downloaded File Properties
    [Reactive] private string _downloadedFile;
    [Reactive] private int _totalParts;
    [Reactive] private int _currentPart;
    [Reactive] private string _downloadStatus;
    #endregion
    #endregion

    #region .ctor
    public MainViewModel()
    {
        _messages = [];
        InitMqtt();
    }
    #endregion

    #region Mqtt Operations
    private async void InitMqtt()
    {
        string broker = "localhost";
        int port = 1883;
        string clientId = Guid.NewGuid().ToString();

        ConnectStatus = "Connection...";
        // Create a MQTT client factory
        var factory = new MqttClientFactory();

        // Create a MQTT client instance
        _mqttClient = factory.CreateMqttClient();

        // Create MQTT client options
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithClientId(clientId)
            .WithCleanSession()
            .Build();

        var connectResult = await _mqttClient.ConnectAsync(options);
        if (connectResult.ResultCode != MqttClientConnectResultCode.Success)
        {
            ConnectStatus = "Not Connected";
            return;
        }
        else
            ConnectStatus = "Connected";

        _mqttFileDownloader = new MqttFileDownloaderBuilder(_mqttClient)
            .WithRootTopic(RootTopic)
            .WithTimeout(3000)
            .WithFolder("")
            .Build();

        _mqttFileDownloader.DownloadCompleted += (o, e) =>
        {
            DownloadStatus = $"Download completed with {e}.";
        };
        _mqttFileDownloader.DownloadProgress += (o, e) =>
        {
            CurrentPart = e.CurrentPart;
            TotalParts = e.TotalPart;
        };
        _mqttFileDownloader.DownloadStarted += (o, e) =>
        {
            DownloadedFile = e;
            DownloadStatus = $"Download started.";
        };
        _mqttFileDownloader.DownloadCancelled += (o, e) =>
        {
            DownloadStatus = $"Download cancelled. With {e}.";
        };
        await _mqttFileDownloader.Listen();
    }
    #endregion
}
