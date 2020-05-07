using NAudio.Wave;
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
   class PlotViewModel : ViewModelBase, IDisposable
   {
      public AppState AppState { get; }
      public List<PlotSampleViewModel> SignalPlotVMs { get; }
      public IEnumerable<ISampleProvider> SampleSignal => SignalPlotVMs.Select(vm => vm.SampleSignal);
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }

      private bool isPlotEnable;
      public PlotViewModel(List<MultiSignalViewModel> multiSignalVMs)
      {
         AppState = Locator.Current.GetService<AppState>();

         SignalPlotVMs = new List<PlotSampleViewModel>(3);
         SignalPlotVMs.AddRange(
            multiSignalVMs.Select(s =>
               new PlotSampleViewModel(
                  new PlotSampleProvider(s.SampleSignal)
               ).DisposeWith(Disposables))
            );

         foreach (var plotVM in SignalPlotVMs)
         {
            AppState.WhenAnyValue(x => x.IsHDPlot)
               .Subscribe(x => plotVM.IsHighDefinition = x)
               .DisposeWith(Disposables);
            this.WhenAnyValue(x => x.IsPlotEnable)
               .Subscribe(x => plotVM.IsPlotEnable = x)
               .DisposeWith(Disposables);
         }

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
