using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   class PresetViewModel : ViewModelBase, IDisposable
   {
      public AppState AppState { get; }
      public List<MultiSignalViewModel> MultiSignalVMs { get; }

      public PlotViewModel PlotViewModel { get; }
      public List<ControlSliderViewModel> MonoVolVMs { get; }
      public SwitchingModeSampleProvider FinalSample { get; }

      public PresetViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();

         MultiSignalVMs = new List<MultiSignalViewModel>(3)
         {
            new MultiSignalViewModel().DisposeWith(Disposables),
            new MultiSignalViewModel().DisposeWith(Disposables),
            new MultiSignalViewModel().DisposeWith(Disposables)
         };

         MonoVolVMs = new List<ControlSliderViewModel>(2)
         {
            ControlSliderViewModel.BasicVol,
            ControlSliderViewModel.BasicVol
         };

         PlotViewModel = new PlotViewModel(MultiSignalVMs);

         FinalSample = new SwitchingModeSampleProvider(
                        PlotViewModel.SampleSignal.Take(1).Single(),
                        PlotViewModel.SampleSignal.Skip(1)
                     );

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

      void Save()
      {
         IEnumerable<Generators.POCOs.MultiSignal> pocos;
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
         var jsonStr = new Generators.POCOs.Preset { MultiSignals = pocos.ToList() }.ToJson();
      }

      void Load()
      {
         //Generators.POCOs.Preset.FromJson<Generators.POCOs.MonoPreset>("jsonStr");
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

