using DynamicData;
using NAudio.Wave;
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
   public class MultiSignalGeneratorViewModel : ViewModelBase, IDisposable
   {
      private string name = "MultiSignalGenerator";
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ReactiveCommand<Unit, Unit> AddCommand { get; }
      public ReactiveCommand<BasicSignalGeneratorViewModel, Unit> RemoveCommand { get; }

      private readonly ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> basicSignalGeneratorVMs;
      public ReadOnlyObservableCollection<BasicSignalGeneratorViewModel> BasicSignalGeneratorVMs => basicSignalGeneratorVMs;
      private SourceCache<BasicSignalGeneratorViewModel, int> BasicSignalGeneratorVMsSourceCache { get; }
      public ISampleProvider SampleSignal => mixingSampleProvider;

      private readonly MultiSignalGenerator mixingSampleProvider;
      public MultiSignalGeneratorViewModel(string firstSignalName = "Signal1")
      {
         BasicSignalGeneratorVMsSourceCache = new SourceCache<BasicSignalGeneratorViewModel, int>(x => x.Id);
         var initVM = CreateVM(firstSignalName, 1);
         mixingSampleProvider = new MultiSignalGenerator(initVM.BasicSignalGenerator.WaveFormat);

         BasicSignalGeneratorVMsSourceCache.Connect()
            .OnItemAdded(vm => mixingSampleProvider.AddMixerInput(vm.BasicSignalGenerator))
            .OnItemRemoved(vm =>
            {
               mixingSampleProvider.RemoveMixerInput(vm.BasicSignalGenerator);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out basicSignalGeneratorVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         BasicSignalGeneratorVMsSourceCache.AddOrUpdate(initVM);

         AddCommand = ReactiveCommand.Create(
            () => AddVM())
            .DisposeWith(Disposables);
         RemoveCommand = ReactiveCommand.Create<BasicSignalGeneratorViewModel>(
            vm => RemoveVM(vm))
            .DisposeWith(Disposables);

         VolControlSliderViewModel = ControlSliderViewModel.BasicVol;
         VolControlSliderViewModel
           .ObservableForProperty(x => x.Value, skipInitial: false)
           .Subscribe(x => Volume = x.Value)
           .DisposeWith(Disposables);
      }

      public double Volume
      {
         get => VolControlSliderViewModel.Value;
         set
         {
            if (mixingSampleProvider.Gain == value) return;
            this.RaisePropertyChanging(nameof(Volume));
            VolControlSliderViewModel.Value = value;
            mixingSampleProvider.Gain = value;
            this.RaisePropertyChanged(nameof(Volume));
         }
      }

      public void AddVM() => AddVM($"Signal{GetNextId() + 1}");
      public void AddVM(string name)
      {
         BasicSignalGeneratorVMsSourceCache.AddOrUpdate(CreateVM(name));
      }

      public void RemoveVM(BasicSignalGeneratorViewModel vm)
      {
         BasicSignalGeneratorVMsSourceCache.Remove(vm);
      }
      private BasicSignalGeneratorViewModel CreateVM(string name, double volume = 0) =>
         new BasicSignalGeneratorViewModel { Name = name, Id = GetNextId(), Volume = volume }
         .DisposeWith(Disposables);

      private int GetNextId() => 
         BasicSignalGeneratorVMsSourceCache.Count == 0 ? 
            0 : BasicSignalGeneratorVMsSourceCache.Keys.Max() + 1;

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
