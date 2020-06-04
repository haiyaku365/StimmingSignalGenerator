using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.Generators
{
   class AudioPlayer : IDisposable
   {
      private IWavePlayer player;
      public AudioPlayer(ISampleProvider sampleProvider)
      {
         SampleProvider = sampleProvider;

         // https://github.com/naudio/NAudio/blob/master/Docs/EnumerateOutputDevices.md
         // WaveOut not getting full device name
         //var waveOutCapabilities = 
         //   Enumerable.Range(0, WaveOut.DeviceCount)
         //   .Select(n=> WaveOut.GetCapabilities(n))
         //   .ToArray();

         // DirectSoundOut not include unplug device and cannot set buffer
         //var devices = DirectSoundOut.Devices;

         // WASAPI Devices cannot set buffer
         var enumerator = new MMDeviceEnumerator();
         AudioDevices =
            enumerator
            .EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active | DeviceState.Unplugged).ToArray();

         // If not use ID to select AudioDevice from AudioDevices 
         // it will be different object and fail combo box initialization
         var defaultDevId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
         AudioDevice = AudioDevices.SingleOrDefault(d => d.ID == defaultDevId);

         enumerator.Dispose();
      }
      public MMDevice[] AudioDevices { get; }
      public MMDevice AudioDevice { get; set; }

      private ISampleProvider sampleProvider;
      public ISampleProvider SampleProvider
      {
         get { return sampleProvider; }
         set
         {
            if (sampleProvider == value) return;
            sampleProvider = value;
         }
      }

      public void Play()
      {
         if (AudioDevice?.State != DeviceState.Active) return;
         if (player == null)
         {
            //player = new WaveOutEvent
            //{
            //   NumberOfBuffers = 4,
            //   DesiredLatency = 100
            //};

            //player = new DirectSoundOut(100) { };
            //TODO expose latency to be configurable from ui
            //TODO and persist latency setting and maybe latest playlist to load
            player = new WasapiOut(AudioDevice, AudioClientShareMode.Exclusive, true, 50) { };

            player.Init(new SampleToWaveProvider(SampleProvider));
         }
         player.Play();
      }

      public void Pause()
      {
         player.Pause();
      }

      public void Stop()
      {
         if (player == null)
            return;
         player.Dispose();
         player = null;
      }

      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // dispose managed state (managed objects)
               Stop();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
         }
      }

      // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~AudioPlayer()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}
