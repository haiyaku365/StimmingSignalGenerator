using NAudio.Wave;
using StimmingSignalGenerator.NAudio.OpenTK.Audio.OpenAL;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.NAudio.Player
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
