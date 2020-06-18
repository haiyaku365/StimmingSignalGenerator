using DynamicData;
using NAudio.Utils;
using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
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
            vm.TrackVMs[1].TimeSpanSecond = 7200;
            vm.TrackVMs[1].GeneratorMode = GeneratorModeType.Stereo;
            vm.TrackVMs[2].Name = "DesignTrackName3";

            return vm;
         }
      }
   }
   public class PlaylistViewModel : ViewModelBase
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public string SavePath { get => savePath; set => this.RaiseAndSetIfChanged(ref savePath, value); }
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
      public double CrossfadeDuration { get => crossfadeDuration; set => this.RaiseAndSetIfChanged(ref crossfadeDuration, value); }
      public bool IsShuffleMode { get => isShuffleMode; set => this.RaiseAndSetIfChanged(ref isShuffleMode, value); }
      public bool IsNoteMode { get => isNoteMode; set => this.RaiseAndSetIfChanged(ref isNoteMode, value); }
      public ISampleProvider FinalSample => volumeSampleProvider;
      public AppState AppState { get; }

      private SourceList<TrackViewModel> TrackVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<TrackViewModel> trackVMs;
      private TrackViewModel selectedTrackVM;
      private TrackViewModel playingTrackVM;
      private TrackViewModel autoplayingTrackVM;
      private bool isTimingMode;
      private double crossfadeDuration;
      private bool isShuffleMode;
      private bool isNoteMode;
      private string name = string.Empty;
      private string note;
      private readonly TimingSwitchSampleProvider timingSwitchSampleProvider;
      private readonly SwitchingSampleProvider switchingSampleProvider;
      private readonly VolumeSampleProviderEx volumeSampleProvider;
      private string savePath;

      public PlaylistViewModel()
      {
         IsTimingMode = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.IsTimingMode, false);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.IsTimingMode, () => IsTimingMode.ToString())
            .DisposeWith(Disposables);
         IsShuffleMode = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.IsShuffleMode, false);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.IsShuffleMode, () => IsShuffleMode.ToString())
            .DisposeWith(Disposables);
         CrossfadeDuration = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.CrossfadeDuration, 0d);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.CrossfadeDuration, () => CrossfadeDuration.ToString())
            .DisposeWith(Disposables);
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
               if (!IsTimingMode)
               {
                  if (trackVM.IsPlaying)
                  {
                     SwitchPlayingTrack(null);
                  }
                  if (trackVM.FinalSample == switchingSampleProvider.SampleProvider ||
                     trackVM.FinalSample == switchingSampleProvider.OldSampleProvider)
                  {
                     switchingSampleProvider.ForceEndCrossfade();
                  }
               }
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

         var MasterVolVMPocoStr = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.MasterVolumeVM, string.Empty);
         if (!string.IsNullOrEmpty(MasterVolVMPocoStr))
         {
            MasterVolVM.SetToPOCO(JsonSerializer.Deserialize<POCOs.ControlSlider>(MasterVolVMPocoStr));
         }
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.MasterVolumeVM, () => JsonSerializer.Serialize(MasterVolVM.ToPOCO()))
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
         this.WhenAnyValue(x => x.CrossfadeDuration)
            .Subscribe(x =>
            {
               switchingSampleProvider.CrossfadeDuration = x;
               timingSwitchSampleProvider.CrossfadeDuration = x;
            })
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.IsShuffleMode)
            .Subscribe(x => timingSwitchSampleProvider.IsShuffleMode = x)
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
            canExecute: this.WhenAnyValue(x => x.SavePath, x => !string.IsNullOrWhiteSpace(x))
            ).DisposeWith(Disposables);
      }

      /// <summary>
      /// Manual switch track playing
      /// </summary>
      /// <param name="trackVM"></param>
      public void SwitchPlayingTrack(TrackViewModel trackVM)
      {
         if (IsTimingMode)
         {
            timingSwitchSampleProvider.ForceSwitch(trackVM?.FinalSample);
         }
         else
         {
            PlayingTrackVM = trackVM;
         }
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
            SavePath = SavePath,
            Note = Note,
            Tracks = TrackVMsSourceList.Items.Select(x => x.ToPOCO()).ToList()
         };
      }

      public Task CopyAsCompressedText()
      {
         var compressedJson = CompressionBase64.Compress(JsonSerializer.Serialize(this.ToPOCO()));
         return Avalonia.Application.Current.Clipboard.SetTextAsync($"{Name}.json{compressedJson}");
      }

      private readonly Regex compressedTextRegex = new Regex(@"^(.*).json(.*)");
      public async Task PasteCompressedText()
      {
         var compressedJson = (await Avalonia.Application.Current.Clipboard.GetTextAsync()).Trim();
         var match = compressedTextRegex.Match(compressedJson);
         if (!match.Success) return;

         LoadFromPoco(
            JsonSerializer.Deserialize<POCOs.Playlist>(
               CompressionBase64.Decompress(match.Groups[2].Value)));
         Name = match.Groups[1].Value;
      }

      public async Task SaveAsync() => await this.ToPOCO().SaveAsync();
      public async Task SaveAsAsync()
      {
         var savePath = await this.ToPOCO().SaveAsAsync();
         if (string.IsNullOrWhiteSpace(savePath) || savePath == this.SavePath) return;
         this.SavePath = savePath;
         this.Name = Path.GetFileName(SavePath);
      }

      public async Task LoadDefaultAsync()
      {
         POCOs.Playlist playlist;
         // Try load playlist before exit if exist
         string savePath = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.PlaylistVM, string.Empty);

         // Save playlist path when exit
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.PlaylistVM, () => SavePath?.ToString() ?? string.Empty)
            .DisposeWith(Disposables);

         playlist = await PlaylistFile.LoadAsync(savePath);
         if (playlist == null)
         {
            // Load first file if failed
            playlist = await PlaylistFile.LoadFirstFileAsync();
         }
         LoadFromPoco(playlist);
      }

      public async Task LoadAsync()
      {
         var playlist = await PlaylistFile.LoadAsync();
         LoadFromPoco(playlist);
      }

      private void LoadFromPoco(POCOs.Playlist poco)
      {
         if (poco == null) return;
         //Clean old stuff
         TrackVMsSourceList.Clear();
         //Load to vm
         Name = poco.Name;
         SavePath = poco.SavePath;
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
