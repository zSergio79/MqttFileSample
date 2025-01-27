﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReconnectSubscriberMqtt.Model
{
    public class DefaultMqttReconnectTimePolicy : IMqttReconnectTimePolicy
    {
        public int GetDelay(int tryNumber) => 1000;
    }
}
