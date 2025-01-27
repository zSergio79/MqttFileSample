using MQTTnet;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

using ReconnectMqttClient;

using ReconnectSubscriberMqtt.Model;

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace ReconnectSubscriberMqtt.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region DI
    private readonly IMqttClient _mqttClient;
    private readonly Subscriber _subscriber;
    #endregion

    #region Bindable Properties
    [Reactive] private string _host = "localhost";
    [Reactive] private string _topic = "reconnectedSample";
    [Reactive] private bool _isConnected = false;
    [Reactive] private ObservableCollection<string> _messages = [];
    [Reactive] private int _maxMessages = 10;
    [Reactive] private bool _isStarted = false;
    #endregion

    #region Commands
    public ReactiveCommand<Unit,Unit> Connect { get; }
    public ReactiveCommand<Unit,Unit> Disconnect { get; }
    public ReactiveCommand<Unit,Unit> Start { get; }
    public ReactiveCommand<Unit,Unit> Stop { get; }
    #endregion

    #region .ctor
    private int reenter = 0;
    public MainViewModel()
    {
        _mqttClient =new ReconnectableMqttClient( new MqttClientFactory().CreateMqttClient());
        _subscriber = new Subscriber(_mqttClient);

        _subscriber.MessageReceived += (s, message) =>
        {
            Messages.Add(message);
            if (Messages.Count > MaxMessages)
                Messages.RemoveAt(0);
        };

        _mqttClient.ConnectedAsync += async (o) =>
        {
            RxApp.MainThreadScheduler.Schedule(_ =>  IsConnected = o.ConnectResult.ResultCode == MqttClientConnectResultCode.Success);
            Debug.WriteLine($"Connected - {o.ConnectResult.ResultCode} - [{DateTime.Now:HH:mm:ss}]");
            if(IsStarted)
                await DoStart();
            reenter = 0;
        };
        _mqttClient.DisconnectedAsync += async (o) => 
        {
            RxApp.MainThreadScheduler.Schedule( _ => 
            {
                IsConnected = false; 
                _subscriber?.StopListen();
            });
            Debug.WriteLine($"Disconnect -- {o.Reason} -- [{DateTime.Now:HH:mm:ss}]");
        };

        #region Commands
        var canListen = this.WhenAnyValue(x => x.IsConnected);
        var canConnect = canListen.Select(x => !x);
        Connect = ReactiveCommand.CreateFromTask(DoConnect, canConnect);
        Disconnect = ReactiveCommand.CreateFromTask(DoDisconnect, canListen);
        Start = ReactiveCommand.CreateFromTask(DoStart, canListen);
        Stop = ReactiveCommand.CreateFromTask(DoStop, canListen);
        #endregion
    }
    #endregion

    #region Commands
    private async Task DoConnect()
    {
        _subscriber.Host = Host;
        await _subscriber.Connect();
    }
    private async Task DoDisconnect()
    {
        await _subscriber.Disconnect();
    }
    private async Task DoStart()
    {
        await _subscriber.StartListen(Topic);
        IsStarted = true;
    }

    private async Task DoStop()
    {
        await _subscriber.StopListen();
        IsStarted = false;
    }
    #endregion
}
