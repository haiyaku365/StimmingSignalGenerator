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

            var count = Constants.Wave.DefaultSampleRate / 4;
            float[] buffer = Array.Empty<float>();
            buffer = BufferHelpers.Ensure(buffer, count);

            vm.FinalSample.Read(buffer, 0, count);

            return vm;
         }
      }
   }
   public class PlaylistViewModel : ViewModelBase
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public ReadOnlyObservableCollection<TrackViewModel> TrackVMs => trackVMs;
      public ControlSliderViewModel MasterVolVM { get; }
      public TrackViewModel SelectedTrackVM { get => selectedTrackVM; set => this.RaiseAndSetIfChanged(ref selectedTrackVM, value); }
      /// <summary>
      /// Current track that play manually
      /// </summary>
      public TrackViewModel PlayingTrackVM { get => playingTrackVM; set => this.RaiseAndSetIfChanged(ref playingTrackVM, value); }
      public bool IsAutoTrackChanging { get => isAutoTrackChanging; set => this.RaiseAndSetIfChanged(ref isAutoTrackChanging, value); }
      public ISampleProvider FinalSample => volumeSampleProvider;
      public AppState AppState { get; }

      private SourceList<TrackViewModel> TrackVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<TrackViewModel> trackVMs;
      private TrackViewModel selectedTrackVM;
      private TrackViewModel playingTrackVM;
      private TrackViewModel autoplayingTrackVM;
      private bool isAutoTrackChanging;
      private string name;
      private readonly TimingSwitchSampleProvider timingSwitchSampleProvider;
      private readonly SwitchingSampleProvider switchingSampleProvider;
      private readonly VolumeSampleProvider volumeSampleProvider;
      public PlaylistViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         TrackVMsSourceList = new SourceList<TrackViewModel>().DisposeWith(Disposables);
         var innerDisposables =
            new List<(TrackViewModel vm, CompositeDisposable disposable)>();
         TrackVMsSourceList.Connect()
            .OnItemAdded(trackVM =>
            {
               timingSwitchSampleProvider.AddSample(trackVM.FinalSample, TimeSpan.FromSeconds(trackVM.TimeSpanSecond));
               var disposable = new CompositeDisposable().DisposeWith(Disposables);
               trackVM
                  .WhenAnyValue(x => x.TimeSpanSecond)
                  .Subscribe(x =>
                     timingSwitchSampleProvider.UpdateTimeSpan(trackVM.FinalSample, TimeSpan.FromSeconds(x))
                  )
                  .DisposeWith(disposable);
               innerDisposables.Add((trackVM, disposable));
            })
            .OnItemRemoved(trackVM =>
            {
               if (trackVM.IsPlaying && !IsAutoTrackChanging)
                  SwitchPlayingTrack(null);
               timingSwitchSampleProvider.RemoveSample(trackVM.FinalSample);
               var innerDisposable = innerDisposables.First(x => x.vm == trackVM);
               innerDisposable.disposable.Dispose();
               innerDisposables.Remove(innerDisposable);
               trackVM.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out trackVMs)
            .Subscribe()
            .DisposeWith(Disposables);


         timingSwitchSampleProvider = new TimingSwitchSampleProvider();
         switchingSampleProvider = new SwitchingSampleProvider { SampleProvider = timingSwitchSampleProvider };
         volumeSampleProvider = new VolumeSampleProvider(switchingSampleProvider);

         MasterVolVM = ControlSliderViewModel.BasicVol.DisposeWith(Disposables);
         MasterVolVM.WhenAnyValue(vm => vm.Value)
            .Subscribe(m => volumeSampleProvider.Volume = (float)m)
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsAutoTrackChanging, x => x.PlayingTrackVM)
            .Subscribe(_ =>
            {
               if (IsAutoTrackChanging)
               {
                  switchingSampleProvider.SampleProvider = timingSwitchSampleProvider;
                  UpdateIsPlaying(autoplayingTrackVM);
               }
               else
               {
                  switchingSampleProvider.SampleProvider = PlayingTrackVM?.FinalSample;
                  UpdateIsPlaying(PlayingTrackVM);
               }
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
            .Subscribe(x =>
            {
               autoplayingTrackVM =
                  TrackVMsSourceList.Items.FirstOrDefault(vm =>
                     vm.FinalSample == x.EventArgs.SampleProvider);
               UpdateIsPlaying(autoplayingTrackVM);
            })
            .DisposeWith(Disposables);
      }

      /// <summary>
      /// Manual switch track playing
      /// </summary>
      /// <param name="trackVM"></param>
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
         var vm = new TrackViewModel().DisposeWith(Disposables);
         vm.AddAndSetName(Constants.ViewModelName.TrackVMName, TrackVMsSourceList);
      }
      public Task AddTrackFromClipboard() =>
         TrackVMsSourceList.AddFromClipboard(Constants.ViewModelName.TrackVMName, Disposables);

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
      public async Task LoadDefaultAsync() => LoadFromPoco(await PlaylistFile.LoadFirstFileAsync());
      public async Task LoadAsync() => LoadFromPoco(await PlaylistFile.LoadAsync());
      private void LoadFromPoco(POCOs.Playlist poco)
      {
         if (poco == null) return;
         //Clean old stuff
         TrackVMsSourceList.Clear();
         //Load to vm
         Name = poco.Name;
         for (int i = 0; i < poco.Tracks.Count; i++)
         {
            var trackVM = TrackViewModel.FromPOCO(poco.Tracks[i]).DisposeWith(Disposables);
            if (trackVM.Name == null) trackVM.SetName(Constants.ViewModelName.TrackVMName, TrackVMsSourceList);
            TrackVMsSourceList.Add(trackVM);
         }
      }

      private void UpdateIsPlaying(TrackViewModel trackViewModel)
      {
         foreach (var trackVM in TrackVMsSourceList.Items) { trackVM.IsPlaying = false; }
         if (trackViewModel == null) return;
         trackViewModel.IsPlaying = true;
      }
   }
}
