using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.NAudio.OxyPlot;
using StimmingSignalGenerator.NAudio.Player;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;

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
      public double FadeInDuration { get => fadeInDuration; set => this.RaiseAndSetIfChanged(ref fadeInDuration, value); }
      public double FadeOutDuration { get => fadeOutDuration; set => this.RaiseAndSetIfChanged(ref fadeOutDuration, value); }

      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }
      public ReactiveCommand<Unit, Unit> TogglePlayCommand { get; }

      public ReactiveCommand<Unit, Unit> SwitchToALAudioPlayerCommand { get; }
      public ReactiveCommand<Unit, Unit> SwitchToWasapiAudioPlayerCommand { get; }
      public ReactiveCommand<Unit, Unit> SwitchToWaveOutAudioPlayerCommand { get; }
      public IAudioPlayer AudioPlayer { get => audioPlayer; private set => this.RaiseAndSetIfChanged(ref audioPlayer, value); }
      public AudioPlayerType CurrentAudioPlayerType => currentAudioPlayerType.Value;
      public bool IsPlaying => isPlaying.Value;

      public AppState AppState { get; }
      public PlotSampleProvider PlotSampleProvider { get; }

      private double fadeInDuration;
      private double fadeOutDuration;
      private ObservableAsPropertyHelper<bool> isPlaying;
      private readonly ISampleProvider sampleProvider;
      private readonly FadeInOutSampleProviderEx fadeInOutSampleProviderEx;
      private IAudioPlayer audioPlayer;
      private ObservableAsPropertyHelper<AudioPlayerType> currentAudioPlayerType;

      public AudioPlayerViewModel(ISampleProvider sourceProvider)
      {
         fadeInOutSampleProviderEx = new FadeInOutSampleProviderEx(sourceProvider, true);
         PlotSampleProvider = new PlotSampleProvider(fadeInOutSampleProviderEx);

         this.sampleProvider = PlotSampleProvider;

         AppState = Locator.Current.GetService<AppState>();

         FadeInDuration = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.FadeInDuration, 0d);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.FadeInDuration, () => FadeInDuration.ToString())
            .DisposeWith(Disposables);
         FadeOutDuration = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.FadeOutDuration, 0d);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.FadeOutDuration, () => FadeOutDuration.ToString())
            .DisposeWith(Disposables);

         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.CurrentAudioPlayerType, () => CurrentAudioPlayerType.ToString())
            .DisposeWith(Disposables);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.Latency, () => AudioPlayer.Latency.ToString())
            .DisposeWith(Disposables);

         if (AppState.OSPlatform == OSPlatform.Windows)
         {
            var audioPlayerType =
               ConfigurationHelper.GetConfigOrDefault(
                  Constants.ConfigKey.CurrentAudioPlayerType,
                  AudioPlayerType.Wasapi);
            SwitchAudioPlayer(audioPlayerType);
         }
         else
         {
            try
            {
               SwitchAudioPlayer(AudioPlayerType.OpenAL);
            }
            catch (DllNotFoundException)
            {
               //OpenAL not available in the system
               throw;
            }
            catch (Exception) { throw; }
         }

         this.WhenAnyValue(x => x.AudioPlayer)
            .Select(x => GetAudioPlayerType(x))
            .ToProperty(this, nameof(CurrentAudioPlayerType), out currentAudioPlayerType)
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.AudioPlayer.PlayerStatus)
            .Select(x => x == PlayerStatus.Play)
            .ToProperty(this, nameof(IsPlaying), out isPlaying)
            .DisposeWith(Disposables);
         Observable.FromEventPattern(
            h => fadeInOutSampleProviderEx.OnFadeOutCompleted += h,
            h => fadeInOutSampleProviderEx.OnFadeOutCompleted -= h)
            // Wait another buffer so last fadeout pass through to audio player
            .Delay(TimeSpan.FromMilliseconds(AudioPlayer.Latency * 2), RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => AudioPlayer.Stop())
            .DisposeWith(Disposables);

         PlayCommand = ReactiveCommand.Create(Play,
            canExecute: this.WhenAnyValue(
               property1: x => x.IsPlaying,
               property2: x => x.AudioPlayer.SelectedAudioDevice,
               selector: (p1, p2) => !p1 && !string.IsNullOrEmpty(p2)))
            .DisposeWith(Disposables);
         StopCommand = ReactiveCommand.Create(Stop,
            canExecute: this.WhenAnyValue(x => x.IsPlaying, selector: x => x))
            .DisposeWith(Disposables);
         TogglePlayCommand = ReactiveCommand.Create(TogglePlay,
            canExecute: this.WhenAnyValue(x => x.AudioPlayer.SelectedAudioDevice, selector: x => !string.IsNullOrEmpty(x)))
            .DisposeWith(Disposables);

         SwitchToALAudioPlayerCommand = ReactiveCommand.Create(
            () => SwitchAudioPlayer(AudioPlayerType.OpenAL),
            canExecute: this.WhenAnyValue(
               property1: x => x.CurrentAudioPlayerType,
               property2: x => x.IsPlaying,
               selector: (type, _) => type != AudioPlayerType.OpenAL && !IsPlaying)
            ).DisposeWith(Disposables);
         SwitchToWasapiAudioPlayerCommand = ReactiveCommand.Create(
            () => SwitchAudioPlayer(AudioPlayerType.Wasapi),
            canExecute: this.WhenAnyValue(
               property1: x => x.CurrentAudioPlayerType,
               property2: x => x.IsPlaying,
               selector: (type, _) => type != AudioPlayerType.Wasapi && !IsPlaying && AppState.OSPlatform == OSPlatform.Windows)
            ).DisposeWith(Disposables);
         SwitchToWaveOutAudioPlayerCommand = ReactiveCommand.Create(
            () => SwitchAudioPlayer(AudioPlayerType.WaveOut),
            canExecute: this.WhenAnyValue(
               property1: x => x.CurrentAudioPlayerType,
               property2: x => x.IsPlaying,
               selector: (type, _) => type != AudioPlayerType.WaveOut && !IsPlaying && AppState.OSPlatform == OSPlatform.Windows)
            ).DisposeWith(Disposables);
      }

      public void SwitchAudioPlayer(AudioPlayerType audioPlayerType)
      {
         // Not switch if already is that type
         switch (audioPlayerType)
         {
            case AudioPlayerType.OpenAL:
               if (AudioPlayer != null && AudioPlayer is ALAudioPlayer) return;
               break;
            case AudioPlayerType.Wasapi:
               if (AudioPlayer != null && AudioPlayer is WasapiAudioPlayer) return;
               break;
            case AudioPlayerType.WaveOut:
               if (AudioPlayer != null && AudioPlayer is WaveOutAudioPlayer) return;
               break;
            case AudioPlayerType.None:
            default:
               return;
         }

         int latency;
         if (AudioPlayer == null)
         {
            // Load form config if init
            latency = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.Latency, AudioPlayerBase.DefaultLatency);
         }
         else
         {
            // Load from previous AudioPlayer
            latency = AudioPlayer.Latency;
         }

         AudioPlayer = CreateAudioPlayer(audioPlayerType);
         AudioPlayer.Latency = latency;
      }

      public AudioPlayerType GetAudioPlayerType(IAudioPlayer audioPlayer)
      {
         if (audioPlayer == null) return AudioPlayerType.None;
         if (audioPlayer is ALAudioPlayer) return AudioPlayerType.OpenAL;
         if (audioPlayer is WasapiAudioPlayer) return AudioPlayerType.Wasapi;
         if (audioPlayer is WaveOutAudioPlayer) return AudioPlayerType.WaveOut;
         return AudioPlayerType.None;
      }

      public IAudioPlayer CreateAudioPlayer(AudioPlayerType audioPlayerType)
      {
         return audioPlayerType switch
         {
            AudioPlayerType.Wasapi => new WasapiAudioPlayer(sampleProvider.ToWaveProvider()).DisposeWith(Disposables),
            AudioPlayerType.WaveOut => new WaveOutAudioPlayer(sampleProvider.ToWaveProvider()).DisposeWith(Disposables),
            AudioPlayerType.OpenAL => new ALAudioPlayer(sampleProvider.ToWaveProvider16()).DisposeWith(Disposables),
            AudioPlayerType.None => null,
            _ => throw new NotImplementedException()
         };
      }

      public void TogglePlay()
      {
         if (!IsPlaying)
            Play();
         else
            Stop();
      }

      public void Play()
      {
         fadeInOutSampleProviderEx.BeginFadeIn(FadeInDuration);
         if (!IsPlaying)
         {
            AudioPlayer.Play();
         }
      }
      public void Stop()
      {
         fadeInOutSampleProviderEx.BeginFadeOut(FadeOutDuration);
      }
   }

   public enum AudioPlayerType
   {
      None,
      OpenAL,
      Wasapi,
      WaveOut
   }

}