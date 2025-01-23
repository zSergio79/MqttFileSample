using MQTTnet;
using MQTTnet.Extensions.Rpc;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MqttFileExchanger
{
    internal class MqttRpcClientTopicGenerationWithParametersStrategy : IMqttRpcClientTopicGenerationStrategy
    {
        #region private fields
        private string? _rootTopic;
        #endregion

        #region .ctor
        public MqttRpcClientTopicGenerationWithParametersStrategy(string? rootTopic = null)
        {
            _rootTopic = rootTopic;
        }
        #endregion

        #region Strategy Implementation
        public MqttRpcTopicPair CreateRpcTopics(TopicGenerationContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (context.MethodName.Contains("/") || context.MethodName.Contains("+") || context.MethodName.Contains("#"))
            {
                throw new ArgumentException("The method name cannot contain /, + or #.");
            }

            var requestTopic = string.IsNullOrEmpty(_rootTopic) ? $"MQTTnet.RPC/{Guid.NewGuid():N}/{context.MethodName}"
                   : $"{_rootTopic.TrimEnd('/')}/{context.MethodName}";
            var responseTopic = requestTopic + "/response";

            if (context.Parameters != null)
            {
                foreach (var parameter in context.Parameters)
                {
                    var payload = JsonSerializer.Serialize(parameter.Value, parameter.Value.GetType());
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic($"{requestTopic}/{parameter.Key}")
                        .WithPayload(payload)
                        .WithQualityOfServiceLevel(context.QualityOfServiceLevel)
                        .Build();
                    context.MqttClient.PublishAsync(message).Wait();
                }
            }

            return new MqttRpcTopicPair
            {
                RequestTopic = requestTopic,
                ResponseTopic = responseTopic
            };
        }
        #endregion
    }
}
