using DynamicData;
using NAudio.Utils;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.MVVM.UiHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
            vm.Note = "Design playlist Note: \r\n" +
               "zH4uUAnv2tejBdAcKf5gyqjGSsd43vdgAfjW5bVC \r\n" +
               "PycwQb596YrnhUgxPtfKM6hEKka6zuYrjC9Rjx37 \r\n" +
               "5wbWfWNgJkyatkfS5vvruUgjzx4UpM5Su2qsAHev \r\n" +
               "QJTYJjgXnVz2XaP2wHDTw9GPVeW5mYCnr2rKzn8z \r\n" +
               "67jDcN8fYD6zVsCRsAe33BSHfGdDUdQq6S8N3jvU \r\n" +
               "4yrkcYsjBy7bVDzAEw2DJm4vvAa8CUZskg9YbNcC \r\n" +
               "m9ryr5XRzuQtTVSAqXkwdUd4Cyn7R6aBs4Z5MNVX \r\n";
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
      public string Note { get => note; set => this.RaiseAndSetIfChanged(ref note, value); }
      public ReadOnlyObservableCollection<TrackViewModel> TrackVMs => trackVMs;
      public ControlSliderViewModel MasterVolVM { get; }
      public TrackViewModel SelectedTrackVM { get => selectedTrackVM; set => this.RaiseAndSetIfChanged(ref selectedTrackVM, value); }
      public ReactiveCommand<Unit, Unit> SaveCommand { get; }
      /// <summary>
      /// Current track that play manually
      /// </summary>
      public TrackViewModel PlayingTrackVM { get => playingTrackVM; set => this.RaiseAndSetIfChanged(ref playingTrackVM, value); }
      public bool IsTimingMode { get => isTimingMode; set => this.RaiseAndSetIfChanged(ref isTimingMode, value); }
      public bool IsNoteMode { get => isNoteMode; set => this.RaiseAndSetIfChanged(ref isNoteMode, value); }
      public ISampleProvider FinalSample => volumeSampleProvider;
      public AppState AppState { get; }

      private SourceList<TrackViewModel> TrackVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<TrackViewModel> trackVMs;
      private TrackViewModel selectedTrackVM;
      private TrackViewModel playingTrackVM;
      private TrackViewModel autoplayingTrackVM;
      private bool isTimingMode;
      private bool isNoteMode;
      private string name = string.Empty;
      private string note;
      private readonly TimingSwitchSampleProvider timingSwitchSampleProvider;
      private readonly SwitchingSampleProvider switchingSampleProvider;
      private readonly VolumeSampleProviderEx volumeSampleProvider;
      private string SavePath;
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
               if (trackVM.IsPlaying && !IsTimingMode)
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
         volumeSampleProvider = new VolumeSampleProviderEx(switchingSampleProvider);

         MasterVolVM = ControlSliderViewModel.BasicVol.DisposeWith(Disposables);
         MasterVolVM.WhenAnyValue(vm => vm.Value)
            .Subscribe(m => volumeSampleProvider.Volume = (float)m)
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsTimingMode, x => x.PlayingTrackVM)
            .Subscribe(_ =>
            {
               if (IsTimingMode)
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
               if (IsNoteMode) IsNoteMode = false;
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

         SaveCommand = ReactiveCommand.CreateFromTask(
            SaveAsync,
            canExecute: this.WhenAnyValue(x => x.Name, x => !string.IsNullOrWhiteSpace(x))
            ).DisposeWith(Disposables);
      }

      /// <summary>
      /// Manual switch track playing
      /// </summary>
      /// <param name="trackVM"></param>
      public void SwitchPlayingTrack(TrackViewModel trackVM)
      {
         if (trackVM == null) return;
         PlayingTrackVM = trackVM;
      }
      private void SwitchPlayingTrackByIndex(int trackVmIndex)
         => SwitchPlayingTrack(TrackVMsSourceList.Items.ElementAtOrDefault(trackVmIndex));

      // Issue CommandParameter not passing when using Hotkey 
      // https://github.com/AvaloniaUI/Avalonia/issues/2446
      #region Hard code workaround
      public void SwitchPlayingTrackByIndex0() => SwitchPlayingTrackByIndex(0);
      public void SwitchPlayingTrackByIndex1() => SwitchPlayingTrackByIndex(1);
      public void SwitchPlayingTrackByIndex2() => SwitchPlayingTrackByIndex(2);
      public void SwitchPlayingTrackByIndex3() => SwitchPlayingTrackByIndex(3);
      public void SwitchPlayingTrackByIndex4() => SwitchPlayingTrackByIndex(4);
      public void SwitchPlayingTrackByIndex5() => SwitchPlayingTrackByIndex(5);
      public void SwitchPlayingTrackByIndex6() => SwitchPlayingTrackByIndex(6);
      public void SwitchPlayingTrackByIndex7() => SwitchPlayingTrackByIndex(7);
      public void SwitchPlayingTrackByIndex8() => SwitchPlayingTrackByIndex(8);
      public void SwitchPlayingTrackByIndex9() => SwitchPlayingTrackByIndex(9);
      #endregion 

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
            Note = Note,
            Tracks = TrackVMsSourceList.Items.Select(x => x.ToPOCO()).ToList()
         };
      }
      public async Task SaveAsync() => await this.ToPOCO().SaveAsync(SavePath);
      public async Task SaveAsAsync()
      {
         SavePath = await this.ToPOCO().SaveAsAsync();
         this.Name = Path.GetFileName(SavePath);
      }

      public async Task LoadDefaultAsync()
      {
         var (playlist, savePath) = await PlaylistFile.LoadFirstFileAsync();
         LoadFromPoco(playlist, savePath);
      }

      public async Task LoadAsync()
      {
         var (playlist, savePath) = await PlaylistFile.LoadAsync();
         LoadFromPoco(playlist, savePath);
      }

      private void LoadFromPoco(POCOs.Playlist poco, string savePath)
      {
         if (poco == null) return;
         //Clean old stuff
         TrackVMsSourceList.Clear();
         //Load to vm
         Name = poco.Name;
         SavePath = savePath;
         Note = poco.Note;
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
