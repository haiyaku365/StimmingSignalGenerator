using NAudio.Wave;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Text;

namespace StimmingSignalGenerator.NAudio
{
   public interface IAudioPlayer : IReactiveObject, IDisposable
   {
      ReadOnlyObservableCollection<string> AudioDevices { get; }
      string SelectedAudioDevice { get; set; }
      int Latency { get; set; }
      public PlayerStatus PlayerStatus { get; }

      void Play();
      void Pause();
      void Stop();
   }
}
