using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReconnectSubscriberMqtt.Model
{
    public interface IMqttReconnectTimePolicy
    {
        int GetDelay(int tryNumber);
    }
}
