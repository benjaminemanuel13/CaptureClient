using CaptureClient.EventArguments;
using CaptureClient.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;

namespace CaptureClient.Services
{
    public class CaptureService : ICatpureService
    {
        public event EventHandler<AudioCapturedEventArgs> CapturedAudio;

        private MediaCapture _cap;
        private LowLagMediaRecording _recording;

        private Stream _baseStream;
        private IRandomAccessStream _stream;

        private MediaFrameReader _reader;
        private MediaFrameSource audioFrameSource;

        public CaptureService() {
            _baseStream = new MemoryStream();
        }

        private IMediaEncodingProperties _properties;

        public async void Init() {
            MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings() {
                StreamingCaptureMode = StreamingCaptureMode.Audio
            };

            _properties = AudioEncodingProperties.CreateMp3(48000, 1, 16);

            _cap = new MediaCapture();
            
            await _cap.InitializeAsync(settings);

            var forms = _cap.AudioDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Audio);

            //await _cap.SetEncodingPropertiesAsync(MediaStreamType.Audio, _properties, new MediaPropertySet());

            foreach (var form in forms) {
                var th = form.Subtype;
            }

            var all = _cap.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio).ToList();

            audioFrameSource = _cap.FrameSources.Where(x => x.Value.Info.MediaStreamType == MediaStreamType.Audio).First().Value;

            var support = audioFrameSource.SupportedFormats.Count();// as IReadOnlyList<MediaFrameFormat>;

            foreach (var format in audioFrameSource.SupportedFormats)
            {
                var value = format.Subtype;
            }

            //var sets = audioFrameSource.SetFormatAsync(_properties);

            //await audioFrameSource.Controller.AudioDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Audio, _properties);

            _reader = await _cap.CreateFrameReaderAsync(audioFrameSource);

            _reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Buffered;
            _reader.FrameArrived += _reader_FrameArrived;

            _cap.Failed += _cap_Failed;
        }

        private void _reader_FrameArrived(MediaFrameReader sender, MediaFrameArrivedEventArgs args)
        {
            var frame = sender.TryAcquireLatestFrame();

            if (frame != null)
            {
                ProcessAudioFrame(frame.AudioMediaFrame);
                EncodeAudio(frame.AudioMediaFrame);
            }
        }

        private AudioGraph graph = null;

        private async Task InitEncode()
        {
            string path = ApplicationData.Current.LocalFolder.Path + "\\";

            StorageFolder tempFolder = await StorageFolder.GetFolderFromPathAsync(path);
            StorageFile file = await tempFolder.CreateFileAsync("remote.wav", CreationCollisionOption.ReplaceExisting);

            AudioEncodingProperties props = AudioEncodingProperties.CreatePcm(48000, 1, 16);

            AudioGraphSettings settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Other)
            {
                EncodingProperties = props
            };
            var res = await AudioGraph.CreateAsync(settings);

            graph = res.Graph;

            await graph.CreateFileOutputNodeAsync(file);

            graph.QuantumProcessed += Graph_QuantumProcessed;

            graph.Start();
        }

        private async void EncodeAudio(AudioMediaFrame frame)
        {
            AudioFrameInputNode node = graph.CreateFrameInputNode();
            node.AddFrame(frame.GetAudioFrame());
        }

        private void Graph_QuantumProcessed(AudioGraph sender, object args)
        {
            sender.ResetAllNodes();
        }

        private async void Record()
        {
            var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            StorageFile file = await localFolder.CreateFileAsync("audio.mp3", CreationCollisionOption.GenerateUniqueName);
            _recording = await _cap.PrepareLowLagRecordToStorageFileAsync(
                    MediaEncodingProfile.CreateMp3(AudioEncodingQuality.High), file);

            await _recording.StartAsync();
        }

        private async void StopRecord()
        {
            await _recording.StopAsync();
        }

        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        unsafe private void ProcessAudioFrame(AudioMediaFrame frame)
        {
            using (AudioFrame audioFrame = frame.GetAudioFrame())
            using (AudioBuffer buffer = audioFrame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                byte[] cshBytes = new byte[capacityInBytes];
                Marshal.Copy(new IntPtr(dataInBytes), cshBytes, 0, (int)capacityInBytes);

                CapturedAudio?.Invoke(this, new AudioCapturedEventArgs(cshBytes));

                string typ = frame.AudioEncodingProperties.Type;
            }
        }

        private void _cap_Failed(MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs)
        {
            
        }

        public async void StartCapture()
        {
            await InitEncode();
            Record();
            await _reader.StartAsync();
        }

        public async void StopCapture()
        {
            StopRecord();
            await _reader.StopAsync();
        }

    }
}
