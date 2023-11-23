using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CaptureClient.ViewModelControllers.Interfaces
{
    internal interface IMainController
    {
        void StartCapturing();
        void StopCapturing();
    }
}
