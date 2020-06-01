using DynamicData;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
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
      public static MultiSignalViewModel Data
      {
         get
         {
            var track = new TrackViewModel();
            return track.MultiSignalVMs[0];
         }
      }
   }
   public class MultiSignalViewModel : ViewModelBase, ISignalTree
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public string FullName => fullName.Value;
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public double Volume { get => volume; set { this.RaiseAndSetIfChanged(ref volume, value); } }
      public ReadOnlyObservableCollection<BasicSignalViewModel> BasicSignalVMs => basicSignalVMs;
      public ISampleProvider SampleSignal => multiSignal;

      public ISignalTree Parent { get; }
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded =>
            DeepSourceListTracker.ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved =>
            DeepSourceListTracker.ObservableItemRemoved;

      private string name = "MultiSignals";
      private readonly ObservableAsPropertyHelper<string> fullName;
      private double volume;
      private DeepSourceListTracker<BasicSignalViewModel> DeepSourceListTracker { get; }
      private SourceList<BasicSignalViewModel> BasicSignalVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> basicSignalVMs;
      private readonly MultiSignal multiSignal;
      public static MultiSignalViewModel FromPOCO(POCOs.MultiSignal poco, ISignalTree parent)
      {
         var multiSignalVM = new MultiSignalViewModel(parent, new MultiSignal());
         multiSignalVM.VolControlSliderViewModel.MinValue = poco.Volume.Min;
         multiSignalVM.VolControlSliderViewModel.MaxValue = poco.Volume.Max;
         multiSignalVM.VolControlSliderViewModel.Value = poco.Volume.Value;

         foreach (var signal in poco.BasicSignals)
         {
            multiSignalVM.AddVM(
               BasicSignalViewModel.FromPOCO(signal, multiSignalVM)
            );
         }
         return multiSignalVM;
      }
      public POCOs.MultiSignal ToPOCO() =>
         new POCOs.MultiSignal()
         {
            Volume = VolControlSliderViewModel.ToPOCO(),
            BasicSignals = BasicSignalVMs.Select(x => x.ToPOCO()).ToList()
         };

      public MultiSignalViewModel(ISignalTree parent) : this(parent, new MultiSignal())
      {
         //init vm
         Add();
         basicSignalVMs.First().Volume = 1;
      }
      public MultiSignalViewModel(ISignalTree parent, MultiSignal multiSignal)
      {
         Parent = parent ?? throw new ArgumentNullException(nameof(parent));
         BasicSignalVMsSourceList = new SourceList<BasicSignalViewModel>().DisposeWith(Disposables);
         this.multiSignal = multiSignal;

         BasicSignalVMsSourceList.Connect()
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

         DeepSourceListTracker =
            new DeepSourceListTracker<BasicSignalViewModel>(BasicSignalVMsSourceList)
            .DisposeWith(Disposables);

         VolControlSliderViewModel = ControlSliderViewModel.BasicVol.DisposeWith(Disposables);
         VolControlSliderViewModel
            .ObservableForProperty(x => x.Value, skipInitial: false)
            .Subscribe(x => Volume = x.Value)
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.Volume, skipInitial: false)
            .Subscribe(_ =>
            {
               VolControlSliderViewModel.Value = Volume;
               multiSignal.Gain = Volume;
            })
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.Name)
            .ToProperty(this, nameof(FullName), out fullName);
      }
      
      public void Add() => AddVM(CreateVM());
      public Task AddFromClipboard() => BasicSignalVMsSourceList.AddFromClipboard(this, BasicSignalVMName, Disposables);
      public void Remove(BasicSignalViewModel vm) => vm.RemoveAndMaintainName(BasicSignalVMName, BasicSignalVMsSourceList);
      private void AddVM(BasicSignalViewModel vm) => vm.AddAndSetName(BasicSignalVMName, BasicSignalVMsSourceList);

      private const string BasicSignalVMName = "Signal";
      private BasicSignalViewModel CreateVM(double volume = 0) =>
         new BasicSignalViewModel(this) { Volume = volume }
         .DisposeWith(Disposables);
   }
}
