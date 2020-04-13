using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class BasicSignalGeneratorViewModel : ViewModelBase, IDisposable
   {
      public BasicSignalGenerator BasicSignalGenerator { get; set; }
      public SignalSliderViewModel FreqSignalSliderViewModel { get; set; }
      public SignalSliderViewModel VolSignalSliderViewModel { get; set; }

      public ViewModelActivator Activator { get; }

      private SignalGeneratorType signalType;
      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
      public BasicSignalGeneratorViewModel()
         : this(SignalSliderViewModel.BasicSignalFreq)
      {
      }
      public BasicSignalGeneratorViewModel(
         SignalSliderViewModel freqSignalSliderViewModel)
         : this(freqSignalSliderViewModel, SignalSliderViewModel.BasicVol)
      {
      }
      public BasicSignalGeneratorViewModel(
         SignalSliderViewModel freqSignalSliderViewModel,
         SignalSliderViewModel volSignalSliderViewModel)
      {
         BasicSignalGenerator = new BasicSignalGenerator();

         FreqSignalSliderViewModel = freqSignalSliderViewModel;
         VolSignalSliderViewModel = volSignalSliderViewModel;

         FreqSignalSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => Frequency = x.Value)
            .DisposeWith(Disposables);
         VolSignalSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => Volume = x.Value)
            .DisposeWith(Disposables);

         SignalType = SignalGeneratorType.Sin;
      }

      public SignalGeneratorType SignalType
      {
         get => signalType;
         set
         {
            this.RaiseAndSetIfChanged(ref signalType, value);
            BasicSignalGenerator.Type = signalType;
         }
      }
      public double Frequency
      {
         get => FreqSignalSliderViewModel.Value;
         set
         {
            if (BasicSignalGenerator.Frequency == value) return;
            this.RaisePropertyChanging(nameof(Frequency));
            FreqSignalSliderViewModel.Value = value;
            BasicSignalGenerator.Frequency = value;
            this.RaisePropertyChanged(nameof(Frequency));
         }
      }
      public double Volume
      {
         get => VolSignalSliderViewModel.Value;
         set
         {
            if (BasicSignalGenerator.Gain == value) return;
            this.RaisePropertyChanging(nameof(Volume));
            VolSignalSliderViewModel.Value = value;
            BasicSignalGenerator.Gain = value;
            this.RaisePropertyChanged(nameof(Volume));
         }
      }


      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // TODO: dispose managed state (managed objects)
               Disposables?.Dispose();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
         }
      }

      // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~BasicSignalGeneratorViewModel()
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
