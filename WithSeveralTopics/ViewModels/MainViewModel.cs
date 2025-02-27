using MQTTnet;

using ReactiveUI;
using ReactiveUI.SourceGenerators;

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace WithSeveralTopics.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    #region DI MqttNet
    private readonly IMqttClient _mqttClient;
    #endregion

    #region private fields
    private List<string> _currentTopics = [];
    #endregion

    #region Bindable Properties
    [Reactive] private ObservableCollection<string> _topics = [];
    [Reactive] private string _topicForAdd = string.Empty;
    [Reactive] private ObservableCollection<string> _lastMessages = [];
    #endregion

    #region Commands
    public ReactiveCommand<Unit,Unit> AddTopic { get; }
    public ReactiveCommand<Unit,Unit> ClearTopics { get; }
    public ReactiveCommand<Unit,Unit> ReSubscribe { get; }
    #endregion

    #region .ctor
    public MainViewModel()
    {
         _mqttClient = new MqttClientFactory().CreateMqttClient();
        RxApp.MainThreadScheduler.Schedule(async () => await StartMqtt());

        AddTopic = ReactiveCommand.Create(AddTopicImpl);
        ClearTopics = ReactiveCommand.Create(ClearTopicsImpl);
        ReSubscribe = ReactiveCommand.CreateFromTask(ReSubscribeImpl);
    }
    #endregion

    #region Commands
    private Unit AddTopicImpl()
    {
        if (!string.IsNullOrEmpty(TopicForAdd))
            Topics.Add(TopicForAdd);
        TopicForAdd = "";
        return Unit.Default;
    }
    private Unit ClearTopicsImpl()
    {
        Topics.Clear(); 
        return Unit.Default;
    }
    private async Task ReSubscribeImpl()
    {
        LastMessages.Clear();
        await ReSubscribeMqtt();
    }
    #endregion

    #region Mqtt Operations
    private async Task ReSubscribeMqtt()
    {
        if(!_mqttClient.IsConnected)
        {
            return;
        }

        foreach(var topic in _currentTopics)
        {
            if(!string.IsNullOrEmpty(topic))
            {
                var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                    .WithTopicFilter(topic)
                    .Build();

                await _mqttClient.UnsubscribeAsync(unsubscribeOptions);
            }
        }

        _currentTopics.Clear();

        var subscribeOptionsBuilder = new MqttClientSubscribeOptionsBuilder();
        foreach(var topic in Topics)
        {
            if(!string.IsNullOrEmpty(topic))
            {
                subscribeOptionsBuilder.WithTopicFilter(topic);
                _currentTopics.Add(topic);
            }
        }
        var subscribeOptions = subscribeOptionsBuilder.Build();
        var result = await _mqttClient.SubscribeAsync(subscribeOptions);
    }
    private async Task StartMqtt()
    {
        var _options = new MqttClientOptionsBuilder()
            .WithCleanStart()
            .WithTcpServer("localhost", 1883)
            .Build();

        var result = await _mqttClient.ConnectAsync(_options);

        if (result.ResultCode == MqttClientConnectResultCode.Success)
        {
            _mqttClient.ApplicationMessageReceivedAsync += MessageHandler;
        }
    }

    private Task MessageHandler(MqttApplicationMessageReceivedEventArgs args)
    {
        if(!args.ApplicationMessage.Payload.IsEmpty)
        {
            LastMessages.Add($"{args.ApplicationMessage.ConvertPayloadToString()} on topic [{args.ApplicationMessage.Topic}]");
        }
        return Task.CompletedTask;
    }
    #endregion
}
