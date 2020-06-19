using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.NAudio.Player
{
   public class WaveOutAudioPlayer : AudioPlayerBase
   {
      public WaveOutAudioPlayer(IWaveProvider waveProvider) : base(waveProvider)
      {
         audioDevices = new ObservableCollection<string>();

         for (int i = 0; i < WaveOut.DeviceCount; i++)
         {
            var caps = WaveOut.GetCapabilities(i);
            audioDevices.Add(caps.ProductName);
         }

         SelectedAudioDevice = AudioDevices.FirstOrDefault();
      }
      public override ReadOnlyObservableCollection<string> AudioDevices =>
         new ReadOnlyObservableCollection<string>(audioDevices);

      private ObservableCollection<string> audioDevices { get; }
      protected override IWavePlayer CreateWavePlayer() =>
         new WaveOutEvent()
         {
            DeviceNumber = AudioDevices.IndexOf(SelectedAudioDevice),
            DesiredLatency = Latency * 2, //waveout latency is total duration of all the buffers
            NumberOfBuffers = 2
         };

   }
}
