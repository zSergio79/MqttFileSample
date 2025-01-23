using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MqttFileExchanger
{
    public class MqttUploadFileProgressEventArgs : EventArgs
    {
        public int CurrentPart { get; set; }
        public int TotalPart { get; set; }
    }
}
