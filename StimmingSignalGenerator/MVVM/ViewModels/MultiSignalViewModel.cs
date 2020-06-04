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
      public ISampleProvider SampleSignal => multiSignal;
      public BasicSignalGroupViewModel SignalsViewModel { get; }

      public ISignalTree Parent { get; }
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded =>
            DeepSourceListTracker.ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved =>
            DeepSourceListTracker.ObservableItemRemoved;

      private string name = string.Empty;
      private readonly ObservableAsPropertyHelper<string> fullName;
      private double volume;
      private DeepSourceListTracker<BasicSignalViewModel> DeepSourceListTracker { get; }
      private readonly MultiSignal multiSignal;
      public static MultiSignalViewModel FromPOCO(POCOs.MultiSignal poco, ISignalTree parent)
      {
         var multiSignalVM = new MultiSignalViewModel(parent, new MultiSignal());
         multiSignalVM.VolControlSliderViewModel.MinValue = poco.Volume.Min;
         multiSignalVM.VolControlSliderViewModel.MaxValue = poco.Volume.Max;
         multiSignalVM.VolControlSliderViewModel.Value = poco.Volume.Value;

         foreach (var signal in poco.BasicSignals)
         {
            multiSignalVM.SignalsViewModel.Add(
               BasicSignalViewModel.FromPOCO(signal, multiSignalVM)
            );
         }
         return multiSignalVM;
      }
      public POCOs.MultiSignal ToPOCO() =>
         new POCOs.MultiSignal()
         {
            Volume = VolControlSliderViewModel.ToPOCO(),
            BasicSignals = SignalsViewModel.SignalVMs.Select(x => x.ToPOCO()).ToList()
         };

      public MultiSignalViewModel(ISignalTree parent) : this(parent, new MultiSignal())
      {
         //init vm
         SignalsViewModel.Add();
         SignalsViewModel.SignalVMs.First().Volume = 1;
      }
      public MultiSignalViewModel(ISignalTree parent, MultiSignal multiSignal)
      {
         Parent = parent ?? throw new ArgumentNullException(nameof(parent));
         this.multiSignal = multiSignal;

         SignalsViewModel = new BasicSignalGroupViewModel(
            parent: this,
            name: Constants.ViewModelName.BasicSignalVMName,
            onSignalVmAdded: vm => multiSignal.AddSignal(vm.BasicSignal),
            onSignalVmRemoved: vm => multiSignal.RemoveSignal(vm.BasicSignal),
            createVM: () => new BasicSignalViewModel(this) { Volume = 0 }
            ).DisposeWith(Disposables);

         DeepSourceListTracker =
            new DeepSourceListTracker<BasicSignalViewModel>(
               SignalsViewModel.SignalVMsObservableList)
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
   }
}
