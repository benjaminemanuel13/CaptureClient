using CaptureClient.ViewModelControllers;
using CaptureClient.ViewModelControllers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace CaptureClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IMainController _controller;

        public MainPage()
        {
            this.InitializeComponent();
            this._controller = new MainController();
        }

        private void StartCapture_Click(object sender, RoutedEventArgs e)
        {
            _controller.StartCapturing();

            StartCapture.IsEnabled = false;
            StopCapture.IsEnabled = true;
        }

        private void StopCapture_Click(object sender, RoutedEventArgs e)
        {
            _controller.StopCapturing();

            StartCapture.IsEnabled = true;
            StopCapture.IsEnabled = false;
        }
    }
}
