using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.NAudio;
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

      public ReactiveCommand<Unit, Unit> PlayCommand { get; }
      public ReactiveCommand<Unit, Unit> StopCommand { get; }
      public ReactiveCommand<Unit, Unit> TogglePlayCommand { get; }

      public ReactiveCommand<Unit, Unit> SwitchToALAudioPlayerCommand { get; }
      public ReactiveCommand<Unit, Unit> SwitchToWasapiAudioPlayerCommand { get; }
      public IAudioPlayer AudioPlayer { get => audioPlayer; private set => this.RaiseAndSetIfChanged(ref audioPlayer, value); }
      public AudioPlayerType CurrentAudioPlayerType => currentAudioPlayerType.Value;
      public AppState AppState { get; }

      private readonly ISampleProvider sampleProvider;
      private IAudioPlayer audioPlayer;
      private ObservableAsPropertyHelper<AudioPlayerType> currentAudioPlayerType;
      public AudioPlayerViewModel(ISampleProvider sampleProvider)
      {
         this.sampleProvider = sampleProvider;

         AppState = Locator.Current.GetService<AppState>();

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
            .ToProperty(this, nameof(CurrentAudioPlayerType), out currentAudioPlayerType);

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

         SwitchToALAudioPlayerCommand = ReactiveCommand.Create(
            () => SwitchAudioPlayer(AudioPlayerType.OpenAL),
            canExecute: this.WhenAnyValue(
               property1: x => x.CurrentAudioPlayerType,
               property2: x => x.AppState.IsPlaying,
               selector: (type, isPlaying) => type != AudioPlayerType.OpenAL && !isPlaying)
            );
         SwitchToWasapiAudioPlayerCommand = ReactiveCommand.Create(
            () => SwitchAudioPlayer(AudioPlayerType.Wasapi),
            canExecute: this.WhenAnyValue(
               property1: x => x.CurrentAudioPlayerType,
               property2: x => x.AppState.IsPlaying,
               selector: (type, isPlaying) => type != AudioPlayerType.Wasapi && !isPlaying && AppState.OSPlatform == OSPlatform.Windows)
            );
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
         return AudioPlayerType.None;
      }

      public IAudioPlayer CreateAudioPlayer(AudioPlayerType audioPlayerType)
      {
         return audioPlayerType switch
         {
            AudioPlayerType.Wasapi => new WasapiAudioPlayer(sampleProvider.ToWaveProvider()).DisposeWith(Disposables),
            AudioPlayerType.OpenAL => new ALAudioPlayer(sampleProvider.ToWaveProvider16()).DisposeWith(Disposables),
            AudioPlayerType.None => null,
            _ => throw new NotImplementedException()
         };
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

   public enum AudioPlayerType
   {
      None,
      OpenAL,
      Wasapi
   }

}