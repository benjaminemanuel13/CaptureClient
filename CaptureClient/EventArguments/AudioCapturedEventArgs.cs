using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureClient.EventArguments
{
    public class AudioCapturedEventArgs : EventArgs
    {
        public byte[] Bytes { get; private set; }

        public AudioCapturedEventArgs(byte[] bytes)
        { 
            Bytes = bytes;
        }
    }
}
