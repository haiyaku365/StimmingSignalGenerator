using ReactiveUI;
using System;
using System.Collections.ObjectModel;

namespace StimmingSignalGenerator.NAudio
{
   public interface IAudioPlayer : IReactiveObject, IDisposable
   {
      ReadOnlyObservableCollection<string> AudioDevices { get; }
      string SelectedAudioDevice { get; set; }
      int Latency { get; set; }
      PlayerStatus PlayerStatus { get; }

      void Play();
      void Pause();
      void Stop();
   }
}
