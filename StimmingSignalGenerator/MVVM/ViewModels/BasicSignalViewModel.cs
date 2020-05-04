using Avalonia.Media;
using DynamicData;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
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
   class BasicSignalViewModel : ViewModelBase, IDisposable
   {
      public int Id { get; internal set; }
      private string name = "SignalGenerator";
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public BasicSignal BasicSignal { get; }
      public ControlSliderViewModel FreqControlSliderViewModel { get; }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ControlSliderViewModel ZCPosControlSliderViewModel { get; }


      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> amSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> AMSignalVMs => amSignalVMs;
      private SourceCache<BasicSignalViewModel, int> AMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddAMCommand { get; }
      public ReactiveCommand<BasicSignalViewModel, Unit> RemoveAMCommand { get; }


      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> fmSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> FMSignalVMs => fmSignalVMs;
      private SourceCache<BasicSignalViewModel, int> FMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddFMCommand { get; }
      public ReactiveCommand<BasicSignalViewModel, Unit> RemoveFMCommand { get; }

      public Brush BGColor { get; }
      public BasicSignalViewModel()
         : this(ControlSliderViewModel.BasicSignalFreq)
      {
      }
      public BasicSignalViewModel(
         ControlSliderViewModel freqControlSliderViewModel)
         : this(freqControlSliderViewModel, ControlSliderViewModel.BasicVol)
      {
      }
      public BasicSignalViewModel(
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel)
         : this(freqControlSliderViewModel, volControlSliderViewModel, ControlSliderViewModel.Vol(0.5)) { }

      public BasicSignalViewModel(
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel,
         ControlSliderViewModel zcPosControlSliderViewModel
         )
      {
         BGColor = GetRandomBrush();
         BasicSignal = new BasicSignal();

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

         SignalType = BasicSignalType.Sin;

         AMSignalVMsSourceCache =
            new SourceCache<BasicSignalViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         AMSignalVMsSourceCache.Connect()
            .OnItemAdded(vm => BasicSignal.AddAMSignal(vm.BasicSignal))
            .OnItemRemoved(vm =>
            {
               BasicSignal.RemoveAMSignal(vm.BasicSignal);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out amSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);
         AddAMCommand = ReactiveCommand.Create(
            () => AMSignalVMsSourceCache.AddOrUpdate(CreateAMVM($"AMSignal{GetNextId(AMSignalVMsSourceCache) + 1}")))
            .DisposeWith(Disposables);
         RemoveAMCommand = ReactiveCommand.Create<BasicSignalViewModel>(
            vm => AMSignalVMsSourceCache.Remove(vm))
            .DisposeWith(Disposables);


         FMSignalVMsSourceCache =
            new SourceCache<BasicSignalViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         FMSignalVMsSourceCache.Connect()
            .OnItemAdded(vm => BasicSignal.AddFMSignal(vm.BasicSignal))
            .OnItemRemoved(vm =>
            {
               BasicSignal.RemoveFMSignal(vm.BasicSignal);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out fmSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);
         AddFMCommand = ReactiveCommand.Create(
            () => FMSignalVMsSourceCache.AddOrUpdate(CreateFMVM($"FMSignal{GetNextId(FMSignalVMsSourceCache) + 1}")))
            .DisposeWith(Disposables);
         RemoveFMCommand = ReactiveCommand.Create<BasicSignalViewModel>(
            vm => FMSignalVMsSourceCache.Remove(vm))
            .DisposeWith(Disposables);
      }

      private BasicSignalType signalType;
      public BasicSignalType SignalType
      {
         get => signalType;
         set
         {
            this.RaiseAndSetIfChanged(ref signalType, value);
            BasicSignal.Type = signalType;
         }
      }
      public double Frequency
      {
         get => FreqControlSliderViewModel.Value;
         set
         {
            if (BasicSignal.Frequency == value) return;
            this.RaisePropertyChanging(nameof(Frequency));
            FreqControlSliderViewModel.Value = value;
            BasicSignal.Frequency = value;
            this.RaisePropertyChanged(nameof(Frequency));
         }
      }
      public double Volume
      {
         get => VolControlSliderViewModel.Value;
         set
         {
            if (BasicSignal.Gain == value) return;
            this.RaisePropertyChanging(nameof(Volume));
            VolControlSliderViewModel.Value = value;
            BasicSignal.Gain = value;
            this.RaisePropertyChanged(nameof(Volume));
         }
      }

      public double ZeroCrossingPosition
      {
         get => ZCPosControlSliderViewModel.Value;
         set
         {
            if (BasicSignal.ZeroCrossingPosition == value) return;
            this.RaisePropertyChanging(nameof(ZeroCrossingPosition));
            ZCPosControlSliderViewModel.Value = value;
            BasicSignal.ZeroCrossingPosition = value;
            this.RaisePropertyChanged(nameof(ZeroCrossingPosition));
         }
      }

      private static readonly Random rand = new Random();
      private Brush GetRandomBrush()
      {
         var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
         return new SolidColorBrush(Color.FromArgb(60, r, g, b));
      }

      private BasicSignalViewModel CreateAMVM(string name, double volume = 0) =>
         new BasicSignalViewModel(
            ControlSliderViewModel.AMSignalFreq)
         { Name = name, Id = GetNextId(AMSignalVMsSourceCache), Volume = 0 }
         .DisposeWith(Disposables);

      private BasicSignalViewModel CreateFMVM(string name, double volume = 0) =>
         new BasicSignalViewModel(
            ControlSliderViewModel.FMSignalFreq,
            new ControlSliderViewModel(0, 0, 100, 1, 1, 5))
         { Name = name, Id = GetNextId(FMSignalVMsSourceCache), Volume = 0 }
         .DisposeWith(Disposables);

      private int GetNextId(SourceCache<BasicSignalViewModel, int> SourceCache) =>
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
      // ~BasicSignalViewModel()
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
