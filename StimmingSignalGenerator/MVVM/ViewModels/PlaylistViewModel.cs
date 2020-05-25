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
using System.Text.RegularExpressions;
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
      private SourceList<TrackViewModel> TrackVMsSourceList { get; }

      public TrackViewModel SelectedTrackVM { get => selectedTrackVM; set => this.RaiseAndSetIfChanged(ref selectedTrackVM, value); }
      public TrackViewModel PlayingTrackVM { get => playingTrackVM; set => this.RaiseAndSetIfChanged(ref playingTrackVM, value); }
      public bool IsAutoTrackChanging { get => isAutoTrackChanging; set => this.RaiseAndSetIfChanged(ref isAutoTrackChanging, value); }
      public ISampleProvider FinalSample => switchingSampleProvider;
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

         TrackVMsSourceList = new SourceList<TrackViewModel>().DisposeWith(Disposables);
         TrackVMsSourceList.Connect()
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

         this.WhenAnyValue(x => x.IsAutoTrackChanging, x => x.PlayingTrackVM)
            .Subscribe(_ =>
            {
               switchingSampleProvider.SampleProvider = IsAutoTrackChanging ?
                  timingSwitchSampleProvider :
                  PlayingTrackVM?.FinalSample;
               UpdateIsPlaying(PlayingTrackVM);
            })
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.SelectedTrackVM)
            .Subscribe(_ =>
            {
               foreach (var vm in TrackVMsSourceList.Items) { vm.IsSelected = false; }
               if (SelectedTrackVM == null) return;
               SelectedTrackVM.IsSelected = true;
            })
            .DisposeWith(Disposables);
         timingSwitchSampleProvider.ObservableOnProgressChanged
            .Subscribe(x =>
            {
               var vm = 
                  TrackVMsSourceList.Items
                     .FirstOrDefault(vm => vm.FinalSample == x.EventArgs.SampleProvider);
               if (vm != null) vm.Progress = x.EventArgs.Progress;
            })
            .DisposeWith(Disposables);
         timingSwitchSampleProvider.ObservableOnSampleProviderChanged
            .Subscribe(x => UpdateIsPlaying(TrackVMsSourceList.Items.FirstOrDefault(vm => vm.FinalSample == x.EventArgs.SampleProvider)))
            .DisposeWith(Disposables);
      }

      public void SwitchPlayingTrack(TrackViewModel trackVM)
      {
         PlayingTrackVM = trackVM;
      }

      public void MoveTrack(int fromIdx, int toIdx)
      {
         if (fromIdx == toIdx) return;
         TrackVMsSourceList.Move(fromIdx, toIdx);
         timingSwitchSampleProvider.MoveSample(fromIdx, toIdx);
      }

      public void AddNewTrack()
      {
         TrackVMsSourceList.Add(
            new TrackViewModel().SetName(TrackVMName, TrackVMsSourceList)
            .DisposeWith(Disposables)
         );
      }
      public async Task AddTrackFromClipboard()
      {
         var vm = await TrackViewModel.PasteFromClipboard();
         if (vm == null) return;
         TrackVMsSourceList.Add(
            vm.SetName(TrackVMName, TrackVMsSourceList).DisposeWith(Disposables)
         );
      }

      private const string TrackVMName = "Track";
      public void RemoveTrack(TrackViewModel trackVM)
      {
         TrackVMsSourceList.Remove(trackVM);
      }

      public POCOs.Playlist ToPOCO()
      {
         return new POCOs.Playlist
         {
            Name = Name,
            Tracks = TrackVMsSourceList.Items.Select(x => x.ToPOCO()).ToList()
         };
      }
      public async Task SaveAsync() => await this.ToPOCO().SaveAsync();

      public async Task LoadAsync()
      {
         var poco = await PlaylistFile.LoadAsync();
         if (poco == null) return;
         //Clean old stuff
         TrackVMsSourceList.Clear();
         //Load to vm
         Name = poco.Name;
         for (int i = 0; i < poco.Tracks.Count; i++)
         {
            var trackVM = TrackViewModel.FromPOCO(poco.Tracks[i]);
            TrackVMsSourceList.Add(trackVM.SetName(TrackVMName, TrackVMsSourceList));
         }
      }

      private void UpdateIsPlaying(TrackViewModel trackViewModel)
      {
         foreach (var trackVM in TrackVMsSourceList.Items) { trackVM.IsPlaying = false; }
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
