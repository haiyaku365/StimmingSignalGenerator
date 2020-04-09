using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class AudioPlayerViewModel
   {
      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }
      public SignalSourceControlViewModel SignalSourceControlViewModel { get; }

      private readonly AudioPlayer audioPlayer;

      public AudioPlayerViewModel(SignalSourceControlViewModel signalSourceControlViewModel)
      {
         SignalSourceControlViewModel = signalSourceControlViewModel;

         audioPlayer = new AudioPlayer(SignalSourceControlViewModel.ToModel());
         PlayCommand = ReactiveCommand.Create(() => Play());
         StopCommand = ReactiveCommand.Create(() => Stop());
      }

      private IDisposable signalChangedSub;
      public void Play()
      {
         signalChangedSub = SignalSourceControlViewModel
           .WhenAnyValue(x => x.Frequency, x => x.Volume, x => x.SignalType)
            .Subscribe(_ =>
            {
               audioPlayer.SignalSourceControl = SignalSourceControlViewModel.ToModel();
            });
         audioPlayer.Play();
      }

      public void Stop()
      {
         signalChangedSub.Dispose();
         audioPlayer.Stop();
      }
   }
}
