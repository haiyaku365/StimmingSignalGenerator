using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using StimmingSignalGenerator.NAudio.OpenToolkit.OpenAL;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.NAudio
{
   class ALAudioPlayer : AudioPlayerBase
   {
      public ALAudioPlayer(IWaveProvider waveProvider) : base(waveProvider)
      {
         audioDevices = new ObservableCollection<string>(ALContextHelper.GetAllDevicesName().ToArray());
         SelectedAudioDevice = ALContextHelper.GetDefaultDeviceName();
      }
      public override ReadOnlyObservableCollection<string> AudioDevices
         => new ReadOnlyObservableCollection<string>(audioDevices);
      private ObservableCollection<string> audioDevices { get; }
      protected override IWavePlayer CreateWavePlayer()
         => new ALWavePlayer(SelectedAudioDevice)
         {
            DesiredLatency = Latency,
            NumberOfBuffers = 2
         };
   }
}
