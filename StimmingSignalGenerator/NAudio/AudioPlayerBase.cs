using NAudio.Wave;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.NAudio
{
   public abstract class AudioPlayerBase : ReactiveObject, IAudioPlayer, IDisposable
   {
      /// <summary>
      /// Wave provider to play. Need to restart player to take effect.
      /// </summary>
      public IWaveProvider WaveProvider { get => waveProvider; set => this.RaiseAndSetIfChanged(ref waveProvider, value); }
      public abstract ReadOnlyObservableCollection<string> AudioDevices { get; }
      public string SelectedAudioDevice { get => selectedAudioDevice; set => this.RaiseAndSetIfChanged(ref selectedAudioDevice, value); }
      /// <summary>
      /// Latency in milliseconds. Need to restart player to take effect.
      /// </summary>
      public int Latency { get => latency; set => this.RaiseAndSetIfChanged(ref latency, value); }
      public PlayerStatus PlayerStatus { get => playerStatus; private set => this.RaiseAndSetIfChanged(ref playerStatus, value); }
      public const int DefaultLatency = 50;
      protected abstract IWavePlayer CreateWavePlayer();

      private string selectedAudioDevice;
      private IWavePlayer player;
      private IWaveProvider waveProvider;
      private int latency;
      private PlayerStatus playerStatus;
      private IObservable<EventPattern<StoppedEventArgs>> ObservablePlaybackStopped =>
         Observable.FromEventPattern<StoppedEventArgs>(
            h => player.PlaybackStopped += h,
            h => player.PlaybackStopped -= h
         );

      public AudioPlayerBase(IWaveProvider waveProvider)
      {
         WaveProvider = waveProvider;
         Latency = DefaultLatency;
         PlayerStatus = PlayerStatus.Stop;
      }

      virtual public void Play()
      {
         PlayerStatus = PlayerStatus.Play;
         if (player == null)
         {
            playerStopDisposable?.Dispose();
            playerStopDisposable = new CompositeDisposable(2).DisposeWith(Disposables);

            player = CreateWavePlayer().DisposeWith(playerStopDisposable);

            player.Init(WaveProvider);
            ObservablePlaybackStopped
               .Subscribe(_ =>
               {
                  playerStopDisposable.Dispose();
                  player = null;
               })
               .DisposeWith(playerStopDisposable);
         }
         player.Play();
      }

      virtual public void Pause()
      {
         PlayerStatus = PlayerStatus.Pause;
         player.Pause();
      }

      virtual public void Stop()
      {
         PlayerStatus = PlayerStatus.Stop;
         player.Stop();
      }

      private CompositeDisposable playerStopDisposable;
      protected CompositeDisposable Disposables { get; } = new CompositeDisposable();
      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // dispose managed state (managed objects)
               Disposables?.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
         }
      }

      // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~PlotSampleViewModel()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }

   }
   public enum PlayerStatus
   {
      Play,
      Pause,
      Stop
   }
}
