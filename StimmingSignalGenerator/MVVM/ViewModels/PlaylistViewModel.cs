using DynamicData;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.MVVM.UiHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignPlaylistViewModel : DesignViewModelBase
   {
      public static PlaylistViewModel Data
      {
         get
         {
            PrepareAppState();
            var vm = new PlaylistViewModel();
            vm.Name = "Design playlist";
            vm.AddNewTrack();
            vm.AddNewTrack();
            vm.AddNewTrack();

            vm.TrackVMs[0].TimeSpanSecond = 1;
            vm.TrackVMs[1].GeneratorMode = GeneratorModeType.Stereo;

            var count = Constants.DefaultSampleRate / 4;
            float[] buffer = Array.Empty<float>();
            buffer = BufferHelpers.Ensure(buffer, count);

            vm.FinalSample.Read(buffer, 0, count);

            return vm;
         }
      }
   }
   public class PlaylistViewModel : ViewModelBase, IDisposable
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }

      private readonly ReadOnlyObservableCollection<TrackViewModel> trackVMs;
      public ReadOnlyObservableCollection<TrackViewModel> TrackVMs => trackVMs;
      private SourceCache<TrackViewModel, int> TrackVMsSourceCache { get; }

      public TrackViewModel SelectedTrackVM { get => selectedTrackVM; set => this.RaiseAndSetIfChanged(ref selectedTrackVM, value); }
      public TrackViewModel PlayingTrackVM { get => playingTrackVM; set => this.RaiseAndSetIfChanged(ref playingTrackVM, value); }
      public bool IsAutoTrackChanging { get => isAutoTrackChanging; set => this.RaiseAndSetIfChanged(ref isAutoTrackChanging, value); }

      public PlotSampleViewModel PlotSampleViewModel { get; }
      public ISampleProvider FinalSample => PlotSampleViewModel.SampleSignal;
      public AppState AppState { get; }

      private TrackViewModel selectedTrackVM;
      private TrackViewModel playingTrackVM;
      private bool isAutoTrackChanging;
      private string name;
      private readonly SwitchingSampleProvider switchingSampleProvider;
      private readonly TimingSwitchSampleProvider timingSwitchSampleProvider;
      public PlaylistViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         TrackVMsSourceCache =
            new SourceCache<TrackViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         TrackVMsSourceCache.Connect()
            .OnItemAdded(vm =>
            {
               timingSwitchSampleProvider.AddSample(vm.FinalSample, TimeSpan.FromSeconds(vm.TimeSpanSecond));
               vm
               .WhenAnyValue(x => x.TimeSpanSecond)
               .Subscribe(x => timingSwitchSampleProvider.UpdateTimeSpan(vm.FinalSample, TimeSpan.FromSeconds(x)))
               .DisposeWith(Disposables);
            })
            .OnItemRemoved(vm =>
            {
               if (vm.IsPlaying && !IsAutoTrackChanging)
                  SwitchPlayingTrack(null);
               timingSwitchSampleProvider.RemoveSample(vm.FinalSample);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out trackVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         timingSwitchSampleProvider = new TimingSwitchSampleProvider();
         switchingSampleProvider = new SwitchingSampleProvider();
         switchingSampleProvider.SampleProvider = timingSwitchSampleProvider;

         PlotSampleViewModel =
            new PlotSampleViewModel(new PlotSampleProvider(switchingSampleProvider))
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsAutoTrackChanging, x => x.PlayingTrackVM)
            .Subscribe(_ =>
            {
               switchingSampleProvider.SampleProvider = IsAutoTrackChanging ?
                  timingSwitchSampleProvider :
                  PlayingTrackVM?.FinalSample;
               UpdateIsPlaying(PlayingTrackVM);
            })
            .DisposeWith(Disposables);
         timingSwitchSampleProvider.ObservableOnSampleProviderChanged
            .Subscribe(x => UpdateIsPlaying(TrackVMsSourceCache.Items.FirstOrDefault(vm => vm.FinalSample == x.EventArgs.SampleProvider)))
            .DisposeWith(Disposables);
         AppState
            .WhenAnyValue(x => x.IsHDPlot)
            .Subscribe(x => PlotSampleViewModel.IsHighDefinition = x)
            .DisposeWith(Disposables);
         AppState
            .WhenAnyValue(x => x.IsPlotEnable)
            .Subscribe(x => PlotSampleViewModel.IsPlotEnable = x)
            .DisposeWith(Disposables);
      }

      public void SwitchPlayingTrack(TrackViewModel trackVM)
      {
         PlayingTrackVM = trackVM;
      }

      public void AddNewTrack()
      {
         TrackVMsSourceCache.AddOrUpdate(
            new TrackViewModel()
            .SetNameAndId("Track", TrackVMsSourceCache)
            .DisposeWith(Disposables)
         );
      }
      public async Task AddTrackFromClipboard()
      {
         var vm = await TrackViewModel.PasteFromClipboard();
         if (vm == null) return;
         vm
            .SetNameAndId("Track", TrackVMsSourceCache)
            .DisposeWith(Disposables);
         TrackVMsSourceCache.AddOrUpdate(vm);
      }

      public void RemoveTrack(TrackViewModel trackVM)
      {
         TrackVMsSourceCache.Remove(trackVM);
      }

      public POCOs.Playlist ToPOCO()
      {
         return new POCOs.Playlist
         {
            Name = Name,
            Tracks = TrackVMsSourceCache.Items.Select(x => x.ToPOCO()).ToList()
         };
      }
      public async Task SaveAsync() => await this.ToPOCO().SaveAsync();

      public async Task LoadAsync()
      {
         var poco = await PlaylistFile.LoadAsync();
         if (poco == null) return;
         //Clean old stuff
         TrackVMsSourceCache.Clear();
         //Load to vm
         Name = poco.Name;
         for (int i = 0; i < poco.Tracks.Count; i++)
         {
            var trackVM = TrackViewModel.FromPOCO(poco.Tracks[i]);
            trackVM.Id = i;
            TrackVMsSourceCache.AddOrUpdate(trackVM);
         }
      }

      private void UpdateIsPlaying(TrackViewModel trackViewModel)
      {
         foreach (var trackVM in TrackVMsSourceCache.Items) { trackVM.IsPlaying = false; }
         if (trackViewModel == null) return;
         trackViewModel.IsPlaying = true;
      }

      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
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
      // ~PlaylistViewModel()
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
}
