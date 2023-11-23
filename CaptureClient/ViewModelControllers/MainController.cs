using CaptureClient.Services;
using CaptureClient.Services.Interfaces;
using CaptureClient.ViewModelControllers.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.UI.Xaml;

namespace CaptureClient.ViewModelControllers
{
    public class MainController : IMainController
    {
        private readonly ICatpureService _capture;

        private readonly FileStream _fs;
        string path = ApplicationData.Current.LocalFolder.Path + "\\";

        public MainController() {
            if (File.Exists(path + "test.wav"))
            {
                File.Delete(path + "test.wav");
            }

            _fs = new FileStream(path + "test.wav", FileMode.Create, FileAccess.ReadWrite);
            WriteWavHeader(_fs, true, 1, 16, 48000, 100);

            _capture = new CaptureService();
            _capture.CapturedAudio += _capture_CapturedAudio;
            _capture.Init();
        }

        private void _capture_CapturedAudio(object sender, EventArguments.AudioCapturedEventArgs e)
        {
            _fs.Write(e.Bytes, 0, e.Bytes.Length);
        }

        public void StartCapturing()
        {
            _capture.StartCapture();
        }

        public void StopCapturing()
        {
            _capture.StopCapture();
            _fs.Flush();
            _fs.Close();

            Transcode();
        }

        private async void Transcode()
        {
            StorageFile remoteAudioPCMFile = await StorageFile.GetFileFromPathAsync(path + "test.wav");
            StorageFolder tempFolder = await StorageFolder.GetFolderFromPathAsync(path);
            StorageFile remoteAudioMP3File = await tempFolder.CreateFileAsync("remote.mp3", CreationCollisionOption.ReplaceExisting);

            MediaEncodingProfile profile = MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto);
            profile.Audio.BitsPerSample = 16;
            profile.Audio.ChannelCount = 1;
            profile.Audio.SampleRate = 48000;

            MediaTranscoder transcoder = new MediaTranscoder();
            var preparedTranscodeResult = await transcoder.PrepareFileTranscodeAsync(remoteAudioPCMFile, remoteAudioMP3File, profile);

            if (preparedTranscodeResult.CanTranscode)
            {
                await preparedTranscodeResult.TranscodeAsync();
            }
            else
            {
                if (remoteAudioPCMFile != null)
                {
                    await remoteAudioPCMFile.DeleteAsync();
                }

                if (remoteAudioMP3File != null)
                {
                    await remoteAudioMP3File.DeleteAsync();
                }

                switch (preparedTranscodeResult.FailureReason)
                {
                    case TranscodeFailureReason.CodecNotFound:
                        //Debug.LogError("Codec not found.");
                        break;
                    case TranscodeFailureReason.InvalidProfile:
                        //Debug.LogError("Invalid profile.");
                        break;
                    default:
                        //Debug.LogError("Unknown failure.");
                        break;
                }
            }
        }

        private void WriteWavHeader(FileStream stream, bool isFloatingPoint, ushort channelCount, ushort bitDepth, int sampleRate, int totalSampleCount)
        {
            stream.Position = 0;

            // RIFF header.
            // Chunk ID.
            stream.Write(Encoding.ASCII.GetBytes("RIFF"), 0, 4);

            // Chunk size.
            stream.Write(BitConverter.GetBytes((bitDepth / 8 * totalSampleCount) + 36), 0, 4);

            // Format.
            stream.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);



            // Sub-chunk 1.
            // Sub-chunk 1 ID.
            stream.Write(Encoding.ASCII.GetBytes("fmt "), 0, 4);

            // Sub-chunk 1 size.
            stream.Write(BitConverter.GetBytes(16), 0, 4);

            // Audio format (floating point (3) or PCM (1)). Any other format indicates compression.
            stream.Write(BitConverter.GetBytes((ushort)(isFloatingPoint ? 3 : 1)), 0, 2);

            // Channels.
            stream.Write(BitConverter.GetBytes(channelCount), 0, 2);

            // Sample rate.
            stream.Write(BitConverter.GetBytes(sampleRate), 0, 4);

            // Bytes rate.
            stream.Write(BitConverter.GetBytes(sampleRate * channelCount * (bitDepth / 8)), 0, 4);

            // Block align.
            stream.Write(BitConverter.GetBytes(channelCount * (bitDepth / 8)), 0, 2);

            // Bits per sample.
            stream.Write(BitConverter.GetBytes(bitDepth), 0, 2);



            // Sub-chunk 2.
            // Sub-chunk 2 ID.
            stream.Write(Encoding.ASCII.GetBytes("data"), 0, 4);

            // Sub-chunk 2 size.
            stream.Write(BitConverter.GetBytes(bitDepth / 8 * totalSampleCount), 0, 4);
        }
    }
}
