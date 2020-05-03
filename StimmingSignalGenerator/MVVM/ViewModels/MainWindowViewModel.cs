using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class MainWindowViewModel : ViewModelBase, IDisposable
   {
      public MultiSignalGeneratorViewModel LeftSignalGeneratorsVM { get; }
      public MultiSignalGeneratorViewModel RightSignalGeneratorsVM { get; }
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public PlotSampleViewModel LeftSignalPlotVM { get; }
      public PlotSampleViewModel RightSignalPlotVM { get; }
      public MainWindowViewModel()
      {
         LeftSignalGeneratorsVM = new MultiSignalGeneratorViewModel();
         RightSignalGeneratorsVM = new MultiSignalGeneratorViewModel();
         RightSignalGeneratorsVM.Volume = 0;

         ISampleProvider leftSignal = LeftSignalGeneratorsVM.SampleSignal;
         ISampleProvider rightSignal = RightSignalGeneratorsVM.SampleSignal;

         leftSignal = new PlotSampleProvider(leftSignal);
         LeftSignalPlotVM = new PlotSampleViewModel(leftSignal as PlotSampleProvider).DisposeWith(Disposables);
         rightSignal = new PlotSampleProvider(rightSignal);
         RightSignalPlotVM = new PlotSampleViewModel(rightSignal as PlotSampleProvider).DisposeWith(Disposables);

         //combine ch
         var multiplexedSignal = new MultiplexingSampleProvider(
            new[] {
               leftSignal , //left ch
               rightSignal },//right ch
            2
            );

         AudioPlayerViewModel = new AudioPlayerViewModel(multiplexedSignal).DisposeWith(Disposables);
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
