using Avalonia.Media;
using DynamicData;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class BasicSignalGeneratorViewModel : ViewModelBase, IDisposable
   {
      public int Id { get; internal set; }
      private string name = "SignalGenerator";
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public BasicSignalGenerator BasicSignalGenerator { get; }
      public ControlSliderViewModel FreqControlSliderViewModel { get; }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ControlSliderViewModel ZCPosControlSliderViewModel { get; }


      private readonly ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> amSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> AMSignalVMs => amSignalVMs;
      private SourceCache<BasicSignalGeneratorViewModel, int> AMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddAMCommand { get; }
      public ReactiveCommand<BasicSignalGeneratorViewModel, Unit> RemoveAMCommand { get; }


      private readonly ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> fmSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> FMSignalVMs => fmSignalVMs;
      private SourceCache<BasicSignalGeneratorViewModel, int> FMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddFMCommand { get; }
      public ReactiveCommand<BasicSignalGeneratorViewModel, Unit> RemoveFMCommand { get; }

      public Brush BGColor { get; }
      public BasicSignalGeneratorViewModel()
         : this(ControlSliderViewModel.BasicSignalFreq)
      {
      }
      public BasicSignalGeneratorViewModel(
         ControlSliderViewModel freqControlSliderViewModel)
         : this(freqControlSliderViewModel, ControlSliderViewModel.BasicVol)
      {
      }
      public BasicSignalGeneratorViewModel(
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel)
         : this(freqControlSliderViewModel, volControlSliderViewModel, ControlSliderViewModel.Vol(0.5)) { }

      public BasicSignalGeneratorViewModel(
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel,
         ControlSliderViewModel zcPosControlSliderViewModel
         )
      {
         BGColor = GetRandomBrush();
         BasicSignalGenerator = new BasicSignalGenerator();

         FreqControlSliderViewModel = freqControlSliderViewModel;
         VolControlSliderViewModel = volControlSliderViewModel;
         ZCPosControlSliderViewModel = zcPosControlSliderViewModel;

         FreqControlSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => Frequency = x.Value)
            .DisposeWith(Disposables);
         VolControlSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => Volume = x.Value)
            .DisposeWith(Disposables);
         ZCPosControlSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => ZeroCrossingPosition = x.Value)
            .DisposeWith(Disposables);

         SignalType = BasicSignalGeneratorType.Sin;

         AMSignalVMsSourceCache =
            new SourceCache<BasicSignalGeneratorViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         AMSignalVMsSourceCache.Connect()
            .OnItemAdded(vm => BasicSignalGenerator.AddAMSignal(vm.BasicSignalGenerator))
            .OnItemRemoved(vm =>
            {
               BasicSignalGenerator.RemoveAMSignal(vm.BasicSignalGenerator);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out amSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);
         AddAMCommand = ReactiveCommand.Create(
            () => AMSignalVMsSourceCache.AddOrUpdate(CreateAMVM($"AMSignal{GetNextId(AMSignalVMsSourceCache) + 1}")))
            .DisposeWith(Disposables);
         RemoveAMCommand = ReactiveCommand.Create<BasicSignalGeneratorViewModel>(
            vm => AMSignalVMsSourceCache.Remove(vm))
            .DisposeWith(Disposables);


         FMSignalVMsSourceCache =
            new SourceCache<BasicSignalGeneratorViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         FMSignalVMsSourceCache.Connect()
            .OnItemAdded(vm => BasicSignalGenerator.AddFMSignal(vm.BasicSignalGenerator))
            .OnItemRemoved(vm =>
            {
               BasicSignalGenerator.RemoveFMSignal(vm.BasicSignalGenerator);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out fmSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);
         AddFMCommand = ReactiveCommand.Create(
            () => FMSignalVMsSourceCache.AddOrUpdate(CreateFMVM($"FMSignal{GetNextId(FMSignalVMsSourceCache) + 1}")))
            .DisposeWith(Disposables);
         RemoveFMCommand = ReactiveCommand.Create<BasicSignalGeneratorViewModel>(
            vm => FMSignalVMsSourceCache.Remove(vm))
            .DisposeWith(Disposables);
      }

      private BasicSignalGeneratorType signalType;
      public BasicSignalGeneratorType SignalType
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
         get => FreqControlSliderViewModel.Value;
         set
         {
            if (BasicSignalGenerator.Frequency == value) return;
            this.RaisePropertyChanging(nameof(Frequency));
            FreqControlSliderViewModel.Value = value;
            BasicSignalGenerator.Frequency = value;
            this.RaisePropertyChanged(nameof(Frequency));
         }
      }
      public double Volume
      {
         get => VolControlSliderViewModel.Value;
         set
         {
            if (BasicSignalGenerator.Gain == value) return;
            this.RaisePropertyChanging(nameof(Volume));
            VolControlSliderViewModel.Value = value;
            BasicSignalGenerator.Gain = value;
            this.RaisePropertyChanged(nameof(Volume));
         }
      }

      public double ZeroCrossingPosition
      {
         get => ZCPosControlSliderViewModel.Value;
         set
         {
            if (BasicSignalGenerator.ZeroCrossingPosition == value) return;
            this.RaisePropertyChanging(nameof(ZeroCrossingPosition));
            ZCPosControlSliderViewModel.Value = value;
            BasicSignalGenerator.ZeroCrossingPosition = value;
            this.RaisePropertyChanged(nameof(ZeroCrossingPosition));
         }
      }

      private static readonly Random rand = new Random();
      private Brush GetRandomBrush()
      {
         var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
         return new SolidColorBrush(Color.FromArgb(60, r, g, b));
      }

      private BasicSignalGeneratorViewModel CreateAMVM(string name, double volume = 0) =>
         new BasicSignalGeneratorViewModel(
            ControlSliderViewModel.AMSignalFreq)
         { Name = name, Id = GetNextId(AMSignalVMsSourceCache), Volume = 0 }
         .DisposeWith(Disposables);

      private BasicSignalGeneratorViewModel CreateFMVM(string name, double volume = 0) =>
         new BasicSignalGeneratorViewModel(
            ControlSliderViewModel.FMSignalFreq,
            new ControlSliderViewModel(0, 0, 100, 1, 1, 5))
         { Name = name, Id = GetNextId(FMSignalVMsSourceCache), Volume = 0 }
         .DisposeWith(Disposables);

      private int GetNextId(SourceCache<BasicSignalGeneratorViewModel, int> SourceCache) =>
         SourceCache.Count == 0 ?
            0 : SourceCache.Keys.Max() + 1;

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
