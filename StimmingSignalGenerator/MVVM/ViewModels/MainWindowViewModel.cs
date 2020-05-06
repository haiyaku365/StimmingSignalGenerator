using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class MainWindowViewModel : ViewModelBase, IDisposable
   {
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public List<MultiSignalViewModel> MultiSignalVMs { get; }
      public List<PlotSampleViewModel> SignalPlotVMs { get; }
      public List<ControlSliderViewModel> MonoVolVMs { get; set; }
      public bool IsHDPlot { get => isHDPlot; set => this.RaiseAndSetIfChanged(ref isHDPlot, value); }
      private bool isHDPlot;
      public GeneratorModeType GeneratorMode
      {
         get => generatorMode;
         set => this.RaiseAndSetIfChanged(ref generatorMode, value);
      }
      private GeneratorModeType generatorMode;
      public MainWindowViewModel()
      {
         MultiSignalVMs = new List<MultiSignalViewModel>(3)
         {
            new MultiSignalViewModel().DisposeWith(Disposables),
            new MultiSignalViewModel().DisposeWith(Disposables),
            new MultiSignalViewModel().DisposeWith(Disposables)
         };

         SignalPlotVMs = new List<PlotSampleViewModel>(3);
         SignalPlotVMs.AddRange(
            MultiSignalVMs.Select(s =>
               new PlotSampleViewModel(
                  new PlotSampleProvider(s.SampleSignal)
               ).DisposeWith(Disposables))
            );

         foreach (var plotVM in SignalPlotVMs)
         {
            this.WhenAnyValue(vm => vm.IsHDPlot)
               .Subscribe(x => plotVM.IsHighDefinition = x)
               .DisposeWith(Disposables);
         }

         var finalSample =
            new SwitchingModeSampleProvider(
               SignalPlotVMs.Take(1).Select(s => s.SampleSignal).Single(),
               SignalPlotVMs.Skip(1).Select(s => s.SampleSignal)
            );

         MonoVolVMs = new List<ControlSliderViewModel>(2)
         {
            ControlSliderViewModel.BasicVol,
            ControlSliderViewModel.BasicVol
         };

         this.WhenAnyValue(vm => vm.GeneratorMode)
            .Subscribe(m => finalSample.GeneratorMode = m)
            .DisposeWith(Disposables);
         MonoVolVMs[0].WhenAnyValue(vm => vm.Value)
            .Subscribe(m => finalSample.MonoLeftVolume = (float)m)
            .DisposeWith(Disposables);
         MonoVolVMs[1].WhenAnyValue(vm => vm.Value)
           .Subscribe(m => finalSample.MonoRightVolume = (float)m)
           .DisposeWith(Disposables);

         AudioPlayerViewModel =
            new AudioPlayerViewModel(finalSample)
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
