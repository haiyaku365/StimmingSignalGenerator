using Avalonia;
using NAudio.Wave;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using StimmingSignalGenerator.Helper;
using System.Reactive.Linq;
using DynamicData;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignTrackViewModel : DesignViewModelBase
   {
      public static TrackViewModel MonoData => CreateTrackViewModel(GeneratorModeType.Mono);
      public static TrackViewModel StereoData => CreateTrackViewModel(GeneratorModeType.Stereo);
      static TrackViewModel CreateTrackViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState();
         return new TrackViewModel { Name = "Track1", GeneratorMode = generatorModeType };
      }
   }
   public class TrackViewModel : ViewModelBase, ISignalTree
   {
      public AppState AppState { get; }
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public string FullName => fullName.Value;
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }
      public bool IsSelected { get => isSelected; set => this.RaiseAndSetIfChanged(ref isSelected, value); }
      public float Progress { get => progress; set => this.RaiseAndSetIfChanged(ref progress, value); }
      public double TimeSpanSecond { get => timeSpanSecond; set => this.RaiseAndSetIfChanged(ref timeSpanSecond, Math.Round(value, 2)); }

      public ReadOnlyObservableCollection<MultiSignalViewModel> MultiSignalVMs => multiSignalVMs;
      public List<ControlSliderViewModel> VolVMs { get; }
      public GeneratorModeType GeneratorMode { get => generatorMode; set => this.RaiseAndSetIfChanged(ref generatorMode, value); }
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded
         => GeneratorMode switch
         {
            GeneratorModeType.Mono =>
               MultiSignalVMsSourceList.Items.First().ObservableBasicSignalViewModelsAdded,
            GeneratorModeType.Stereo =>
               Observable.Merge(
                  MultiSignalVMsSourceList.Items.Skip(1).Select(x => x.ObservableBasicSignalViewModelsAdded)),
            _ => throw new NotImplementedException()
         };
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved
         => GeneratorMode switch
         {
            GeneratorModeType.Mono =>
               MultiSignalVMsSourceList.Items.First().ObservableBasicSignalViewModelsRemoved,
            GeneratorModeType.Stereo =>
                Observable.Merge(
                  MultiSignalVMsSourceList.Items.Skip(1).Select(x => x.ObservableBasicSignalViewModelsRemoved)),
            _ => throw new NotImplementedException()
         };

      public IObservableList<BasicSignalViewModel> AllSubBasicSignalVMs
         => AllSubBasicSignalVMsSourceList.AsObservableList();
      public ISignalTree Parent => null;
      public ISampleProvider FinalSample => sample;

      private SourceList<MultiSignalViewModel> MultiSignalVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<MultiSignalViewModel> multiSignalVMs;
      private SourceList<BasicSignalViewModel> AllSubBasicSignalVMsSourceList { get; }
      private readonly SwitchingModeSampleProvider sample;
      private GeneratorModeType generatorMode;
      private string name = string.Empty;
      private bool isPlaying;
      private bool isSelected;
      private float progress;
      private double timeSpanSecond = 0;
      private ReplaySubject<Unit> initCompleteSignal = new ReplaySubject<Unit>();
      private readonly ObservableAsPropertyHelper<string> fullName;
      public static TrackViewModel FromPOCO(POCOs.Track poco)
      {
         var vm = new TrackViewModel(isFromPOCO: true);
         vm.Name = poco.Name;
         vm.TimeSpanSecond = poco.TimeSpanSecond;
         vm.SetupVolumeControlSlider(poco.Volumes.Select(x => ControlSliderViewModel.FromPOCO(x)).ToArray());
         vm.SetupSwitchingModeSignal(poco.MultiSignals.Select(x => MultiSignalViewModel.FromPOCO(x, vm)).ToArray());
         vm.initCompleteSignal.OnNext(Unit.Default);
         vm.initCompleteSignal.OnCompleted();
         return vm;
      }
      public POCOs.Track ToPOCO()
      {
         IEnumerable<POCOs.MultiSignal> signalPocos;
         IEnumerable<POCOs.ControlSlider> volPocos;
         switch (GeneratorMode)
         {
            case GeneratorModeType.Mono:
               signalPocos = MultiSignalVMsSourceList.Items.Take(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Take(2).Select(vm => vm.ToPOCO());
               break;
            case GeneratorModeType.Stereo:
               signalPocos = MultiSignalVMsSourceList.Items.Skip(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Skip(2).Take(1).Select(vm => vm.ToPOCO());
               break;
            default:
               throw new ApplicationException("Bad GeneratorMode");
         }
         return new POCOs.Track
         {
            Name = Name,
            MultiSignals = signalPocos.ToList(),
            Volumes = volPocos.ToList(),
            TimeSpanSecond = TimeSpanSecond
         };
      }
      public TrackViewModel() : this(isFromPOCO: false) { }
      private TrackViewModel(bool isFromPOCO)
      {
         AppState = Locator.Current.GetService<AppState>();

         sample = new SwitchingModeSampleProvider();

         VolVMs = new List<ControlSliderViewModel>();

         this.WhenAnyValue(x => x.Name)
            .ToProperty(this, nameof(FullName), out fullName);

         MultiSignalVMsSourceList =
            new SourceList<MultiSignalViewModel>()
            .DisposeWith(Disposables);
         MultiSignalVMsSourceList.Connect()
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out multiSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         AllSubBasicSignalVMsSourceList =
            new SourceList<BasicSignalViewModel>()
            .DisposeWith(Disposables);

         SetupVolumeControlSlider(
            new[] {
               ControlSliderViewModel.BasicVol,
               ControlSliderViewModel.BasicVol,
               ControlSliderViewModel.BasicVol }
            );

         // Add signal when load from POCO will confuse the sync signal loading logic
         // name will be duplicate and it will pickup wrong object
         SetupSwitchingModeSignal(
            new[] {
               new MultiSignalViewModel(this,addFirstSignal: !isFromPOCO),
               new MultiSignalViewModel(this,addFirstSignal: !isFromPOCO),
               new MultiSignalViewModel(this,addFirstSignal: !isFromPOCO) });

         var GeneratorModeChangedDisposable = new CompositeDisposable().DisposeWith(Disposables);

         Observable.Merge(
            //MultiSignalVMsSourceList.CountChanged.Where(x => x > 0).Select(x => Unit.Default),
            this.WhenAnyValue(x => x.GeneratorMode).Select(x => Unit.Default),
            initCompleteSignal.Amb(Observable.Timer(TimeSpan.FromMilliseconds(100)).Select(x => Unit.Default))
            )
            //.DelaySubscription(TimeSpan.FromMilliseconds(100),RxApp.TaskpoolScheduler)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
               sample.GeneratorMode = GeneratorMode;
               // clean and switch mode
               AllSubBasicSignalVMsSourceList.Clear();
               GeneratorModeChangedDisposable.Dispose();
               GeneratorModeChangedDisposable = new CompositeDisposable().DisposeWith(Disposables);

               // resub to new mode
               this.ObservableBasicSignalViewModelsAdded
                  .Subscribe(x => AllSubBasicSignalVMsSourceList.Add(x))
                  .DisposeWith(GeneratorModeChangedDisposable);
               this.ObservableBasicSignalViewModelsRemoved
                  .Subscribe(x => AllSubBasicSignalVMsSourceList.Remove(x))
                  .DisposeWith(GeneratorModeChangedDisposable);
            })
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsPlaying)
            .Subscribe(_ => { if (!IsPlaying) Progress = 0; })
            .DisposeWith(Disposables);

         initCompleteSignal.Amb(Observable.Timer(TimeSpan.FromMilliseconds(100)).Select(x => Unit.Default))
            .Subscribe(_ =>
            {
               VolVMs[0].WhenAnyValue(vm => vm.Value)
                  .Subscribe(m => sample.MonoLeftVolume = (float)m)
                  .DisposeWith(Disposables);
               VolVMs[1].WhenAnyValue(vm => vm.Value)
                  .Subscribe(m => sample.MonoRightVolume = (float)m)
                  .DisposeWith(Disposables);
               VolVMs[2].WhenAnyValue(vm => vm.Value)
                  .Subscribe(m => sample.StereoVolume = (float)m)
                  .DisposeWith(Disposables);
            }).DisposeWith(Disposables);
      }

      private void SetupSwitchingModeSignal(MultiSignalViewModel[] multiSignalVMs)
      {
         var multiSignalVMsCount = multiSignalVMs.Count();
         switch (multiSignalVMsCount)
         {
            case 1:
               //mono
               GeneratorMode = GeneratorModeType.Mono;
               MultiSignalVMsSourceList.Clear();
               MultiSignalVMsSourceList.AddRange(new[]
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  new MultiSignalViewModel(this).DisposeWith(Disposables),
                  new MultiSignalViewModel(this).DisposeWith(Disposables)
               });
               break;
            case 2:
               //stereo
               GeneratorMode = GeneratorModeType.Stereo;
               MultiSignalVMsSourceList.Clear();
               MultiSignalVMsSourceList.AddRange(new[]
               {
                  new MultiSignalViewModel(this).DisposeWith(Disposables),
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
               });
               break;
            case 3:
               //load all
               MultiSignalVMsSourceList.Clear();
               MultiSignalVMsSourceList.AddRange(new[]
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
                  multiSignalVMs[2].DisposeWith(Disposables),
               });
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetupMultiSignal(params MultiSignalViewModel[] multiSignalVMs)");
         }
         MultiSignalVMsSourceList.Items.ElementAt(0).Name = Constants.ViewModelName.MonoMultiSignalName;
         MultiSignalVMsSourceList.Items.ElementAt(1).Name = Constants.ViewModelName.LeftMultiSignalName;
         MultiSignalVMsSourceList.Items.ElementAt(2).Name = Constants.ViewModelName.RightMultiSignalName;
         sample.MonoSampleProvider = MultiSignalVMsSourceList.Items.Take(1).Single().SampleSignal;
         sample.StereoSampleProviders = MultiSignalVMsSourceList.Items.Skip(1).Select(x => x.SampleSignal);

      }

      private void SetupVolumeControlSlider(ControlSliderViewModel[] controlSliderVMs)
      {
         if (controlSliderVMs == null) controlSliderVMs = Array.Empty<ControlSliderViewModel>();
         VolVMs.Clear();
         switch (controlSliderVMs.Length)
         {
            case 2://mono
               VolVMs.AddRange(
                  new[] {
                     controlSliderVMs[0],
                     controlSliderVMs[1],
                     ControlSliderViewModel.BasicVol
                  });
               break;
            case 1://stereo
               VolVMs.AddRange(
                  new[] {
                     controlSliderVMs[0],
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol
                  });
               break;
            case 3://load all
               VolVMs.AddRange(controlSliderVMs);
               break;
            case 0:
               VolVMs.AddRange(
                  new[] {
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol,
                     ControlSliderViewModel.BasicVol
                  });
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetVolumesFromPOCOs(POCOs.ControlSlider[] pocoVols)");
         }
      }

      public async Task CopyToClipboard()
      {
         var poco = this.ToPOCO();
         var json = JsonSerializer.Serialize(poco, new JsonSerializerOptions { WriteIndented = true });
         await Avalonia.Application.Current.Clipboard.SetTextAsync(json);
      }
      public static async Task<TrackViewModel> PasteFromClipboard()
      {
         var json = await Avalonia.Application.Current.Clipboard.GetTextAsync();
         if (string.IsNullOrWhiteSpace(json)) return null;
         try
         {
            var poco = JsonSerializer.Deserialize<POCOs.Track>(json);
            if (typeof(POCOs.Track).GetProperties().All(x => x.GetValue(poco).IsNullOrDefault())) return null;
            return TrackViewModel.FromPOCO(poco);
         }
         catch (JsonException)
         {
            return null;
         }
      }
   }
}
