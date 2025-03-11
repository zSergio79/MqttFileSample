using Avalonia.Controls;
using Avalonia.Platform.Storage;

using MqttFileExchanger;

using MQTTnet;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace FileUploadWithMqtt.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region DI
    public  Window? Host;

    private IMqttClient _mqttClient;
    private IMqttFileUploader _mqttFileUploader;
    #endregion

    #region Const
    private const string Topic = "intiled_controlx/Office/state";
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> ImportFile { get; }
    #endregion

    #region Bindable Properties
    [Reactive] private string _connectStatus;
    [Reactive] private string _message;
    [Reactive] private int _totalParts;
    [Reactive] private int _currentPart;
    [Reactive] private string _importedFile;
    #endregion

    #region .ctor
    public MainViewModel()
    {
        ImportFile = ReactiveCommand.CreateFromTask(DoImportFile);

        RxApp.MainThreadScheduler.Schedule(InitMqtt);
    }
    #endregion

    #region Commands
    private async Task DoImportFile()
    {
        if (Host == null) return;

        var topLevel = TopLevel.GetTopLevel(Host);
        if (topLevel == null) return;

        var storageProvider = topLevel.StorageProvider;
        if (storageProvider == null) return;

        var files = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.All]
        });

        var file = files.FirstOrDefault();
        if (file == null) return;

        var filename = file.TryGetLocalPath();
        if(string.IsNullOrEmpty(filename)) return;

        ImportedFile = filename;
        CurrentPart = 0;
        var result = await _mqttFileUploader.UploadFile(filename);

        ConnectStatus = $"Upload is {result}";
        CurrentPart = 0;
        TotalParts = 100;
    }
    #endregion

    #region Mqtt Operations
    private async void InitMqtt()
    {
        string broker = "192.168.40.60";
        int port = 1883;
        string clientId = Guid.NewGuid().ToString();

         ConnectStatus = "Connection...";
        // Create a MQTT client factory
        var factory = new MqttClientFactory();
        // Create a MQTT client instance
        _mqttClient = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithTcpServer(broker, port) // MQTT broker address and port
            .WithClientId(clientId)
            .WithCleanSession()
            .Build();

        var connectResult = await _mqttClient.ConnectAsync(options);
        ConnectStatus = connectResult.ResultCode != MqttClientConnectResultCode.Success ? "Uploader Not Connected" : "Uploader Connected";

        _mqttFileUploader = new MqttFileUploaderBuilder(_mqttClient)
           .WithRootTopic(Topic)
           .WithTimeout(1000)
           .Build();

        _mqttFileUploader.UploadFileProgress += (o, e) =>
        {
            TotalParts = e.TotalPart;
            CurrentPart = e.CurrentPart;
        };
    }
    #endregion
}
