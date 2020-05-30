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
      public static MultiSignalViewModel Data => new MultiSignalViewModel();
   }
   public class MultiSignalViewModel : ViewModelBase, ISignalTree
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public double Volume { get => volume; set { this.RaiseAndSetIfChanged(ref volume, value); } }
      public ReadOnlyObservableCollection<BasicSignalViewModel> BasicSignalVMs => basicSignalVMs;
      public ISampleProvider SampleSignal => multiSignal;

      public ISignalTree Parent { get; set; }
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded =>
            DeepSourceListTracker.ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved =>
            DeepSourceListTracker.ObservableItemRemoved;


      private string name = "MultiSignals";
      private double volume;
      private DeepSourceListTracker<BasicSignalViewModel> DeepSourceListTracker { get; }
      private SourceList<BasicSignalViewModel> BasicSignalVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> basicSignalVMs;
      private readonly MultiSignal multiSignal;
      public static MultiSignalViewModel FromPOCO(POCOs.MultiSignal poco)
      {
         var multiSignalVM = new MultiSignalViewModel(new MultiSignal());
         multiSignalVM.VolControlSliderViewModel.MinValue = poco.Volume.Min;
         multiSignalVM.VolControlSliderViewModel.MaxValue = poco.Volume.Max;
         multiSignalVM.VolControlSliderViewModel.Value = poco.Volume.Value;

         foreach (var signal in poco.BasicSignals)
         {
            multiSignalVM.AddVM(
               BasicSignalViewModel.FromPOCO(signal), multiSignalVM
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

      public MultiSignalViewModel() : this(new MultiSignal())
      {
         //init vm
         Add();
         basicSignalVMs.First().Volume = 1;
      }
      public MultiSignalViewModel(MultiSignal multiSignal)
      {
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
            .Subscribe(_ =>
            {
               //Update name 
               foreach (var signalVM in BasicSignalVMsSourceList.Items)
               {
                  signalVM.SetName(BasicSignalVMName, BasicSignalVMsSourceList);
               }
            })
            .DisposeWith(Disposables);

      }

      public void Add() => AddVM(CreateVM(), this);
      public Task AddFromClipboard() => BasicSignalVMsSourceList.AddFromClipboard(this, BasicSignalVMName);
      public void Remove(BasicSignalViewModel vm) => BasicSignalVMsSourceList.Remove(vm);
      private void AddVM(BasicSignalViewModel vm, ISignalTree parent)
      {
         vm.Parent = parent;
         BasicSignalVMsSourceList.Add(vm);
      }

      private static string GetBasicSignalVMName(string name) => $"{name}.Signal";
      private string BasicSignalVMName => GetBasicSignalVMName(Name);
      private BasicSignalViewModel CreateVM(double volume = 0) =>
         new BasicSignalViewModel { Volume = volume }
         .SetName(BasicSignalVMName, BasicSignalVMsSourceList)
         .DisposeWith(Disposables);
   }
}
