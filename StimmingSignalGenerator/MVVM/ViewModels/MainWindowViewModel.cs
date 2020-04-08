using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class MainWindowViewModel : ViewModelBase
   {
      SignalSourceControlViewModel SignalSourceControlViewModel { get; }
      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }
      public MainWindowViewModel()
      {
         SignalSourceControlViewModel = new SignalSourceControlViewModel();


         PlayCommand = ReactiveCommand.Create(
            () => Play()
            //TODO Move player to service
            //this.WhenAnyValue(x => x.player.PlaybackState, x => x != PlaybackState.Playing)
         );
         StopCommand = ReactiveCommand.Create(
            () => Stop()
            //this.WhenAnyValue(x => x.player.PlaybackState, x => x == PlaybackState.Playing)
         );

      }
      private IWavePlayer player;
      public void Play()
      {
         var signalGen = new DynamicSignalGenerator(new NAudio.Wave.SampleProviders.SignalGenerator()
         {
            Frequency = SignalSourceControlViewModel.Frequency,
            Gain = SignalSourceControlViewModel.Volume,
            Type = SignalSourceControlViewModel.SignalType
         });

         SignalSourceControlViewModel
            .WhenAnyValue(x => x.Frequency, x => x.Volume, x => x.SignalType)
            .Subscribe(
                ((double f, double v, SignalGeneratorType t) x) =>
                {
                   signalGen.SourceSignal =
                      new NAudio.Wave.SampleProviders.SignalGenerator()
                      {
                         Frequency = x.f,
                         Gain = x.v,
                         Type = x.t
                      };
                }
            );

         if (player == null)
         {
            var waveOutEvent = new WaveOutEvent();
            waveOutEvent.NumberOfBuffers = 2;
            waveOutEvent.DesiredLatency = 100;
            player = waveOutEvent;
            player.Init(new SampleToWaveProvider(signalGen));
         }
         player.Play();
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
