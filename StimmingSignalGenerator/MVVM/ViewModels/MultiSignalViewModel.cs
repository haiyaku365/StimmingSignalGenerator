using DynamicData;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.MVVM.UiHelper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignMultiSignalViewModel : DesignViewModelBase
   {
      public static MultiSignalViewModel Data => new MultiSignalViewModel();
   }
   public class MultiSignalViewModel : ViewModelBase, IDisposable
   {
      private string name = "MultiSignal";
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ReactiveCommand<Unit, Unit> AddCommand { get; }
      public ReactiveCommand<Unit, Unit> AddFromClipboardCommand { get; }
      public ReactiveCommand<BasicSignalViewModel, Unit> RemoveCommand { get; }

      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> basicSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> BasicSignalVMs => basicSignalVMs;
      private SourceCache<BasicSignalViewModel, int> BasicSignalVMsSourceCache { get; }
      public ISampleProvider SampleSignal => multiSignal;

      private readonly MultiSignal multiSignal;

      public static MultiSignalViewModel FromPOCO(POCOs.MultiSignal poco)
      {
         var multiSignalVM = new MultiSignalViewModel(new MultiSignal());
         multiSignalVM.VolControlSliderViewModel.MinValue = poco.Volume.Min;
         multiSignalVM.VolControlSliderViewModel.MaxValue = poco.Volume.Max;
         multiSignalVM.VolControlSliderViewModel.Value = poco.Volume.Value;

         foreach (var signal in poco.BasicSignals)
         {
            BasicSignalViewModel.FromPOCO(signal)
               .SetNameAndId(BasicSignalVMName, multiSignalVM.BasicSignalVMsSourceCache)
               .AddTo(multiSignalVM.BasicSignalVMsSourceCache);
         }
         return multiSignalVM;
      }

      public POCOs.MultiSignal ToPOCO() =>
         new POCOs.MultiSignal()
         {
            Volume = VolControlSliderViewModel.ToPOCO(),
            BasicSignals = BasicSignalVMs.Select(x => x.ToPOCO()).ToList()
         };

      public MultiSignalViewModel() : this(new MultiSignal())
      {
         //init vm
         AddVM();
         basicSignalVMs.First().Volume = 1;
      }
      public MultiSignalViewModel(MultiSignal multiSignal)
      {
         BasicSignalVMsSourceCache =
            new SourceCache<BasicSignalViewModel, int>(x => x.Id)
            .DisposeWith(Disposables);
         this.multiSignal = multiSignal;

         BasicSignalVMsSourceCache.Connect()
            .OnItemAdded(vm => multiSignal.AddSignal(vm.BasicSignal))
            .OnItemRemoved(vm =>
            {
               multiSignal.RemoveSignal(vm.BasicSignal);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out basicSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         AddCommand = ReactiveCommand
            .Create(AddVM)
            .DisposeWith(Disposables);
         AddFromClipboardCommand = ReactiveCommand
            .CreateFromTask(AddVMFromClipboard)
            .DisposeWith(Disposables);
         RemoveCommand = ReactiveCommand
            .Create<BasicSignalViewModel>(RemoveVM)
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
            if (multiSignal.Gain == value) return;
            this.RaisePropertyChanging(nameof(Volume));
            VolControlSliderViewModel.Value = value;
            multiSignal.Gain = value;
            this.RaisePropertyChanged(nameof(Volume));
         }
      }

      private void AddVM() => CreateVM().AddTo(BasicSignalVMsSourceCache);
      private Task AddVMFromClipboard() => BasicSignalVMsSourceCache.AddFromClipboard(BasicSignalVMName);
      public void RemoveVM(BasicSignalViewModel vm)
      {
         BasicSignalVMsSourceCache.Remove(vm);
      }

      private const string BasicSignalVMName = "Signal";
      private BasicSignalViewModel CreateVM(double volume = 0) =>
            new BasicSignalViewModel { Volume = volume }
            .SetNameAndId(BasicSignalVMName, BasicSignalVMsSourceCache)
         .DisposeWith(Disposables);

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
