using NAudio.CoreAudioApi;
using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignAudioPlayerViewModel : DesignViewModelBase
   {
      public static AudioPlayerViewModel Data => new AudioPlayerViewModel(new BasicSignal());
   }
   public class AudioPlayerViewModel : ViewModelBase
   {
      public MMDevice[] AudioDevices => audioPlayer.AudioDevices;
      public MMDevice AudioDevice
      {
         get => audioPlayer.AudioDevice;
         set
         {
            if (audioPlayer.AudioDevice == value) return;
            this.RaisePropertyChanging(nameof(AudioDevice));
            audioPlayer.AudioDevice = value;
            this.RaisePropertyChanged(nameof(AudioDevice));
         }
      }
      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }

      private readonly AudioPlayer audioPlayer;
      public AppState AppState { get; }
      public AudioPlayerViewModel(ISampleProvider sampleProvider)
      {
         AppState = Locator.Current.GetService<AppState>();

         audioPlayer = new AudioPlayer(sampleProvider).DisposeWith(Disposables);
         PlayCommand = ReactiveCommand.Create(() => Play()).DisposeWith(Disposables);
         StopCommand = ReactiveCommand.Create(() => Stop()).DisposeWith(Disposables);
      }

      public void Play()
      {
         AppState.IsPlaying = true;
         audioPlayer.Play();
      }

      public void Stop()
      {
         AppState.IsPlaying = false;
         audioPlayer.Stop();
      }
   }
}
