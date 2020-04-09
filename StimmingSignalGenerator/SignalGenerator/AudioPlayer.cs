using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   class AudioPlayer
   {
      private IWavePlayer player;
      private readonly DynamicSignalGenerator signalGenerator;
      public AudioPlayer(SignalSourceControl signalSourceControl)
      {
         signalGenerator = new DynamicSignalGenerator(
            new NAudio.Wave.SampleProviders.SignalGenerator()
            {
               Frequency = signalSourceControl.Frequency,
               Gain = signalSourceControl.Volume,
               Type = signalSourceControl.SignalType
            }
         );
      }

      private SignalSourceControl signalSourceControl;
      public SignalSourceControl SignalSourceControl
      {
         get => signalSourceControl;
         set
         {
            if (signalSourceControl == value) return;
            signalSourceControl = value;
            signalGenerator.SourceSignal =
               new NAudio.Wave.SampleProviders.SignalGenerator()
               {
                  Frequency = SignalSourceControl.Frequency,
                  Gain = SignalSourceControl.Volume,
                  Type = SignalSourceControl.SignalType
               };
         }
      }

      public void Play()
      {
         if (player == null)
         {
            var waveOutEvent = new WaveOutEvent();
            waveOutEvent.NumberOfBuffers = 2;
            waveOutEvent.DesiredLatency = 100;
            player = waveOutEvent;
            player.Init(new SampleToWaveProvider(signalGenerator));
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
