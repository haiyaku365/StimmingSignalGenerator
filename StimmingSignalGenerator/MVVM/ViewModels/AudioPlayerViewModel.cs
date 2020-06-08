using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignAudioPlayerViewModel : DesignViewModelBase
   {
      public static AudioPlayerViewModel Data
      {
         get
         {
            PrepareAppState();
            return new AudioPlayerViewModel(new BasicSignal());
         }
      }
   }
   public class AudioPlayerViewModel : ViewModelBase
   {
      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }
      public ReactiveCommand<Unit, Unit> TogglePlayCommand { get; }

      public IAudioPlayer AudioPlayer { get; }
      public AppState AppState { get; }

      public AudioPlayerViewModel(ISampleProvider sampleProvider)
      {
         AppState = Locator.Current.GetService<AppState>();

         AudioPlayer = new WasapiAudioPlayer(sampleProvider.ToWaveProvider16()).DisposeWith(Disposables);
         this.WhenAnyValue(x => x.AudioPlayer.PlayerStatus)
            .Subscribe(x => AppState.IsPlaying = x == PlayerStatus.Play)
            .DisposeWith(Disposables);

         PlayCommand = ReactiveCommand.Create(Play,
            canExecute: AppState.WhenAnyValue(x => x.IsPlaying, selector: x => !x))
            .DisposeWith(Disposables);
         StopCommand = ReactiveCommand.Create(Stop,
            canExecute: AppState.WhenAnyValue(x => x.IsPlaying))
            .DisposeWith(Disposables);
         TogglePlayCommand = ReactiveCommand.Create(TogglePlay,
            canExecute: this.WhenAnyValue(x => x.AudioPlayer.SelectedAudioDevice, selector: x => !string.IsNullOrEmpty(x)))
            .DisposeWith(Disposables);
      }

      public void TogglePlay()
      {
         if (AppState.IsPlaying)
            Stop();
         else
            Play();
      }

      public void Play()
      {
         AudioPlayer.Play();
      }
      public void Stop()
      {
         AudioPlayer.Stop();
      }
   }
}
