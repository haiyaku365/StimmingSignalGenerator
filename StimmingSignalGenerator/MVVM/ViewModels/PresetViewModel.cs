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
   class PresetViewModel : ViewModelBase, IDisposable
   {
      public AppState AppState { get; }
      public List<MultiSignalViewModel> MultiSignalVMs { get => multiSignalVMs; private set => this.RaiseAndSetIfChanged(ref multiSignalVMs, value); }
      public PlotViewModel PlotViewModel { get => plotViewModel; private set => this.RaiseAndSetIfChanged(ref plotViewModel, value); }
      public List<ControlSliderViewModel> MonoVolVMs { get; }
      public SwitchingModeSampleProvider FinalSample { get; }

      public ReactiveCommand<Unit, Unit> SavePresetCommand { get; }
      public ReactiveCommand<Unit, Unit> LoadPresetCommand { get; }

      private PlotViewModel plotViewModel;
      private List<MultiSignalViewModel> multiSignalVMs;
      public PresetViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         SavePresetCommand = ReactiveCommand.CreateFromTask(SaveAsync).DisposeWith(Disposables);
         LoadPresetCommand = ReactiveCommand.CreateFromTask(LoadAsync).DisposeWith(Disposables);

         FinalSample = new SwitchingModeSampleProvider();

         MonoVolVMs = new List<ControlSliderViewModel>(2)
         {
            ControlSliderViewModel.BasicVol,
            ControlSliderViewModel.BasicVol
         };

         SetupMultiSignal(
            new MultiSignalViewModel(),
            new MultiSignalViewModel(),
            new MultiSignalViewModel());

         AppState.WhenAnyValue(x => x.GeneratorMode)
            .Subscribe(x =>
            {
               FinalSample.GeneratorMode = x;
            })
            .DisposeWith(Disposables);
         MonoVolVMs[0].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => FinalSample.MonoLeftVolume = (float)m)
            .DisposeWith(Disposables);
         MonoVolVMs[1].WhenAnyValue(vm => vm.Value)
           .Subscribe(m => FinalSample.MonoRightVolume = (float)m)
           .DisposeWith(Disposables);

      }

      private void SetupMultiSignal(params MultiSignalViewModel[] multiSignalVMs)
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
               throw new ApplicationException("somthing wrong in PresetViewModel.SetupMultiSignal(params MultiSignalViewModel[] multiSignalVMs)");
         }
         PlotViewModel = new PlotViewModel(MultiSignalVMs);
         FinalSample.MonoSampleProvider = PlotViewModel.SampleSignal.Take(1).Single();
         FinalSample.StereoSampleProviders = PlotViewModel.SampleSignal.Skip(1);
      }


      async Task SaveAsync()
      {
         IEnumerable<POCOs.MultiSignal> pocos;
         switch (AppState.GeneratorMode)
         {
            case GeneratorModeType.Mono:
               pocos = MultiSignalVMs.Take(1).Select(vm => vm.ToPOCO());
               break;
            case GeneratorModeType.Stereo:
               pocos = MultiSignalVMs.Skip(1).Select(vm => vm.ToPOCO());
               break;
            default:
               throw new ApplicationException("Bad GeneratorMode");
         }
         await new POCOs.Preset { MultiSignals = pocos.ToList() }.SavePresetAsync();
      }

      async Task LoadAsync()
      {
         var poco = await PresetFile.LoadPresetAsync();
         if (poco == null) return;
         //Clean old stuff
         foreach (var vm in MultiSignalVMs) { vm.Dispose(); }
         PlotViewModel?.Dispose();
         //Load to vm
         SetupMultiSignal(poco.MultiSignals.Select(x => MultiSignalViewModel.FromPOCO(x)).ToArray());
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

