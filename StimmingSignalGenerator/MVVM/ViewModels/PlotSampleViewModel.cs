using NAudio.Wave;
using OxyPlot;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   class PlotSampleViewModel : ViewModelBase, IDisposable
   {
      public PlotModel PlotModel { get; }
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }
      public bool IsHighDefinition { get => isHighDefinition; set => this.RaiseAndSetIfChanged(ref isHighDefinition, value); }
      public ISampleProvider SampleSignal => plotSampleProvider;

      private bool isPlotEnable;
      private bool isHighDefinition;
      private readonly PlotSampleProvider plotSampleProvider;
      public PlotSampleViewModel(PlotSampleProvider plotSampleProvider)
      {
         this.plotSampleProvider = plotSampleProvider;
         PlotModel = plotSampleProvider.PlotModel;

         this.ObservableForProperty(x => x.IsPlotEnable)
            .Subscribe(x => plotSampleProvider.IsEnable = x.Value)
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.IsHighDefinition)
            .Subscribe(x => plotSampleProvider.IsHighDefinition = x.Value)
            .DisposeWith(Disposables);
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
      // ~PlotSampleViewModel()
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
