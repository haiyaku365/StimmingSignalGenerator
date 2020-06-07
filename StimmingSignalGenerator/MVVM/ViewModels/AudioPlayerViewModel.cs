using NAudio.CoreAudioApi;
using NAudio.Wave;
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

      public MMDevice[] AudioDevices => audioPlayer.AudioDevices;
      public MMDevice SelectedAudioDevice { get => selectedAudioDevice; set => this.RaiseAndSetIfChanged(ref selectedAudioDevice, value); }
      public int Latency { get => latency; set => this.RaiseAndSetIfChanged(ref latency, value); }
      public AppState AppState { get; }

      private int latency;
      private MMDevice selectedAudioDevice;
      private readonly AudioPlayer audioPlayer;
      public AudioPlayerViewModel(ISampleProvider sampleProvider)
      {
         AppState = Locator.Current.GetService<AppState>();

         audioPlayer = new AudioPlayer(sampleProvider).DisposeWith(Disposables);

         SelectedAudioDevice = audioPlayer.AudioDevice;
         Latency = audioPlayer.Latency;

         this.WhenAnyValue(x => x.SelectedAudioDevice)
            .Subscribe(_ => audioPlayer.AudioDevice = SelectedAudioDevice)
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.Latency)
            .Subscribe(_ => audioPlayer.Latency = Latency)
            .DisposeWith(Disposables);

         PlayCommand = ReactiveCommand.Create(() => Play(), AppState.WhenAnyValue(x => x.IsPlaying, x => !x))
            .DisposeWith(Disposables);
         StopCommand = ReactiveCommand.Create(() => Stop(), AppState.WhenAnyValue(x => x.IsPlaying))
            .DisposeWith(Disposables);
         TogglePlayCommand = ReactiveCommand.Create(() =>
         {
            if (AppState.IsPlaying)
            {
               Stop();
            }
            else
            {
               Play();
            }
         }).DisposeWith(Disposables);

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
