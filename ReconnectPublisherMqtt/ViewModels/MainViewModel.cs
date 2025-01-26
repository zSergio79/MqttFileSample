using MQTTnet;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

using ReconnectPublisherMqtt.Model;

using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ReconnectPublisherMqtt.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region DI
    private IMqttClient? _mqttClient;
    private Publisher? _publisher;
    #endregion

    #region Bindable Properties
    [Reactive] private string _host = "localhost";
    [Reactive] private int _port = 1883;
    [Reactive] private string _topic = "reconnectedSample";
    [Reactive] private bool _isStaticMessage = false;
    [Reactive] private string _message = string.Empty;
    [Reactive] private bool _isConnected = false;
    #endregion

    #region Commands
    public ReactiveCommand<Unit, Unit> Connect { get; }
    public ReactiveCommand<Unit, Unit> Start { get; }
    public ReactiveCommand<Unit, Unit> Stop { get; }
    #endregion

    #region .ctor
    public MainViewModel()
    {
        _mqttClient = new MqttClientFactory().CreateMqttClient();
        
        _mqttClient.DisconnectedAsync += async (o) => 
        {
            IsConnected = false;
            _publisher.Stop();
        };
        _publisher = new Publisher(_mqttClient)
        {
            Topic = Topic,
            GetMessage = (x) => $"Tick #{x}"
        };

        #region Commands
        var canStart = this.WhenAnyValue(x => x.IsConnected);

        Connect = ReactiveCommand.CreateFromTask(DoConnect);

        Start = ReactiveCommand.Create(DoStart, canStart);
        Stop = ReactiveCommand.Create(DoStop, canStart);
        #endregion
    }
    #endregion

    #region Commands
    private async Task DoConnect()
    {
        if (string.IsNullOrEmpty(_host)) return;

        var options = new MqttClientOptionsBuilder()
            .WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500)
            .WithCleanStart()
            .WithTcpServer(_host, _port)
            .Build();

        try
        {
            IsConnected = (await _mqttClient.ConnectAsync(options)).ResultCode == MqttClientConnectResultCode.Success;
        }
        catch
        {
            IsConnected = false;
        }
    }

    private void DoStart() => _publisher.Start();
    private void DoStop() => _publisher?.Stop();
    #endregion
}
