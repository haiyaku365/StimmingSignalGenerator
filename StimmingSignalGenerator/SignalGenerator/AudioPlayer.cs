using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class AudioPlayer
   {
      private IWavePlayer player;
      public AudioPlayer(ISampleProvider sampleProvider)
      {
         SampleProvider = sampleProvider;
      }

      private ISampleProvider sampleProvider;

      public ISampleProvider SampleProvider
      {
         get { return sampleProvider; }
         set {
            if (sampleProvider == value) return;
            sampleProvider = value;
         }
      }

      public void Play()
      {
         if (player == null)
         {
            var waveOutEvent = new WaveOutEvent();
            waveOutEvent.NumberOfBuffers = 4;
            waveOutEvent.DesiredLatency = 100;
            player = waveOutEvent;
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
   }
}
