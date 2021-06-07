using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pocitani_zprav_fb
{
    class Message
    {
        public string sender_name { get; set; }
        public string content { get; set; }
        public long timestamp_ms { get; set; }
    }
}
