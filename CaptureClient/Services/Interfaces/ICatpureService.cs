using CaptureClient.EventArguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureClient.Services.Interfaces
{
    internal interface ICatpureService
    {
        event EventHandler<AudioCapturedEventArgs> CapturedAudio;

        void Init();
        void StartCapture();
        void StopCapture();
    }
}
