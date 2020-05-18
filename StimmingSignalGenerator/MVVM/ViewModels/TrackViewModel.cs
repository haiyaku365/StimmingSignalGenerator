using Avalonia;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.FileService;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignTrackViewModel : DesignViewModelBase
   {
      public static TrackViewModel MonoData => CreateTrackViewModel(GeneratorModeType.Mono);
      public static TrackViewModel StereoData => CreateTrackViewModel(GeneratorModeType.Stereo);
      static TrackViewModel CreateTrackViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState(generatorModeType);
         return new TrackViewModel { Name = "Track1" };
      }
   }
   public class TrackViewModel : ViewModelBase, IDisposable
   {
      public AppState AppState { get; }
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public List<MultiSignalViewModel> MultiSignalVMs { get => multiSignalVMs; private set => this.RaiseAndSetIfChanged(ref multiSignalVMs, value); }
      public PlotViewModel PlotViewModel { get => plotViewModel; private set => this.RaiseAndSetIfChanged(ref plotViewModel, value); }
      public List<ControlSliderViewModel> VolVMs { get; }
      public SwitchingModeSampleProvider FinalSample { get; }

      public ReactiveCommand<Unit, Unit> SaveTrackCommand { get; }
      public ReactiveCommand<Unit, Unit> LoadTrackCommand { get; }

      private PlotViewModel plotViewModel;
      private List<MultiSignalViewModel> multiSignalVMs;
      private string name;
      public TrackViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         SaveTrackCommand = ReactiveCommand.CreateFromTask(SaveAsync).DisposeWith(Disposables);
         LoadTrackCommand = ReactiveCommand.CreateFromTask(LoadAsync).DisposeWith(Disposables);

         FinalSample = new SwitchingModeSampleProvider();

         VolVMs = new List<ControlSliderViewModel>(3)
         {
            ControlSliderViewModel.BasicVol,
            ControlSliderViewModel.BasicVol,
            ControlSliderViewModel.BasicVol
         };

         SetupSwitchingModeSignal(
            new MultiSignalViewModel(),
            new MultiSignalViewModel(),
            new MultiSignalViewModel());

         AppState.WhenAnyValue(x => x.GeneratorMode)
            .Subscribe(x =>
            {
               FinalSample.GeneratorMode = x;
            })
            .DisposeWith(Disposables);
         VolVMs[0].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => FinalSample.MonoLeftVolume = (float)m)
            .DisposeWith(Disposables);
         VolVMs[1].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => FinalSample.MonoRightVolume = (float)m)
            .DisposeWith(Disposables);
         VolVMs[2].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => FinalSample.StereoVolume = (float)m)
            .DisposeWith(Disposables);

      }

      private void SetupSwitchingModeSignal(params MultiSignalViewModel[] multiSignalVMs)
      {
         var multiSignalVMsCount = multiSignalVMs.Count();
         switch (multiSignalVMsCount)
         {
            case 1:
               //mono
               AppState.GeneratorMode = GeneratorModeType.Mono;
               MultiSignalVMs = new List<MultiSignalViewModel>(3)
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  new MultiSignalViewModel().DisposeWith(Disposables),
                  new MultiSignalViewModel().DisposeWith(Disposables)
               };
               break;
            case 2:
               //stereo
               AppState.GeneratorMode = GeneratorModeType.Stereo;
               MultiSignalVMs = new List<MultiSignalViewModel>(3)
               {
                  new MultiSignalViewModel().DisposeWith(Disposables),
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
               };
               break;
            case 3:
               //load all
               MultiSignalVMs = new List<MultiSignalViewModel>(3)
               {
                  multiSignalVMs[0].DisposeWith(Disposables),
                  multiSignalVMs[1].DisposeWith(Disposables),
                  multiSignalVMs[2].DisposeWith(Disposables),
               };
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetupMultiSignal(params MultiSignalViewModel[] multiSignalVMs)");
         }
         PlotViewModel = new PlotViewModel(MultiSignalVMs);
         FinalSample.MonoSampleProvider = PlotViewModel.SampleSignal.Take(1).Single();
         FinalSample.StereoSampleProviders = PlotViewModel.SampleSignal.Skip(1);
      }

      private void SetVolumesFromPOCOs(POCOs.ControlSlider[] pocoVols)
      {
         if (pocoVols == null) return;
         switch (pocoVols.Length)
         {

            case 2://mono
            case 3://load all
               for (int i = 0; i < pocoVols.Length; i++)
               {
                  VolVMs[i].SetToPOCO(pocoVols[i]);
               }
               break;
            case 1:
               //stereo
               VolVMs[2].SetToPOCO(pocoVols[0]);
               break;
            case 0://no load
               break;
            default:
               //somthing wrong
               throw new ApplicationException("somthing wrong in TrackViewModel.SetVolumesFromPOCOs(POCOs.ControlSlider[] pocoVols)");
         }
      }

      public POCOs.Track ToPOCO()
      {
         IEnumerable<POCOs.MultiSignal> signalPocos;
         IEnumerable<POCOs.ControlSlider> volPocos;
         switch (AppState.GeneratorMode)
         {
            case GeneratorModeType.Mono:
               signalPocos = MultiSignalVMs.Take(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Take(2).Select(vm => vm.ToPOCO());
               break;
            case GeneratorModeType.Stereo:
               signalPocos = MultiSignalVMs.Skip(1).Select(vm => vm.ToPOCO());
               volPocos = VolVMs.Skip(2).Take(1).Select(vm => vm.ToPOCO());
               break;
            default:
               throw new ApplicationException("Bad GeneratorMode");
         }
         return new POCOs.Track
         {
            Name = Name,
            MultiSignals = signalPocos.ToList(),
            Volumes = volPocos.ToList()
         };
      }

      async Task SaveAsync() => await this.ToPOCO().SaveTrackAsync();

      async Task LoadAsync()
      {
         var poco = await TrackFile.LoadTrackAsync();
         if (poco == null) return;
         //Clean old stuff
         foreach (var vm in MultiSignalVMs) { vm.Dispose(); }
         PlotViewModel?.Dispose();
         //Load to vm
         Name = poco.Name;
         SetupSwitchingModeSignal(poco.MultiSignals.Select(x => MultiSignalViewModel.FromPOCO(x)).ToArray());
         SetVolumesFromPOCOs(poco.Volumes?.ToArray());
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
      // ~MainWindowViewModel()
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

