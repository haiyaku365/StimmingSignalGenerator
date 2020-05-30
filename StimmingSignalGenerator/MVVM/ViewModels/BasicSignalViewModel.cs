using Avalonia.Media;
using DynamicData;
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
using System.Text.Json;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignBasicSignalViewModel : DesignViewModelBase
   {
      public static BasicSignalViewModel Data =>
         new BasicSignalViewModel
         {
            Name = $"Signal{random.Next(0, 100)}",
            SignalType = GetRandomEnum<BasicSignalType>(),
            Frequency = random.Next(300, 7000),
            Volume = random.NextDouble(),
            ZeroCrossingPosition = random.NextDouble(),
            IsExpanded = true
         };
   }
   public class BasicSignalViewModel : ViewModelBase,
      INamable, ISignalTree, IDeepSourceList<BasicSignalViewModel>
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public Brush BGColor { get; }
      public BasicSignal BasicSignal { get; }
      public ControlSliderViewModel FreqControlSliderViewModel { get; }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ControlSliderViewModel ZCPosControlSliderViewModel { get; }

      public BasicSignalType SignalType { get => signalType; set { this.RaiseAndSetIfChanged(ref signalType, value); } }
      public double Frequency { get => frequency; set { this.RaiseAndSetIfChanged(ref frequency, value); } }
      public double Volume { get => volume; set { this.RaiseAndSetIfChanged(ref volume, value); } }
      public double ZeroCrossingPosition { get => zeroCrossingPosition; set { this.RaiseAndSetIfChanged(ref zeroCrossingPosition, value); } }

      public bool IsExpanded { get => isExpanded; set => this.RaiseAndSetIfChanged(ref isExpanded, value); }
      public bool IsAMExpanded { get => isAMExpanded; set => this.RaiseAndSetIfChanged(ref isAMExpanded, value); }
      public bool IsFMExpanded { get => isFMExpanded; set => this.RaiseAndSetIfChanged(ref isFMExpanded, value); }

      public ISignalTree Parent { get; set; }
      public IObservable<BasicSignalViewModel> ObservableItemAdded => DeepSourceListTracker.ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableItemRemoved => DeepSourceListTracker.ObservableItemRemoved;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded => ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved => ObservableItemRemoved;

      public ReadOnlyObservableCollection<BasicSignalViewModel> AMSignalVMs => amSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> FMSignalVMs => fmSignalVMs;

      private string name;
      private BasicSignalType signalType;
      private double frequency;
      private double volume;
      private double zeroCrossingPosition;
      private bool isExpanded;
      private bool isAMExpanded;
      private bool isFMExpanded;
      private SourceList<BasicSignalViewModel> AMSignalVMsSourceList { get; }
      private SourceList<BasicSignalViewModel> FMSignalVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> amSignalVMs;
      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> fmSignalVMs;
      private DeepSourceListTracker<BasicSignalViewModel> DeepSourceListTracker { get; }
      public static BasicSignalViewModel FromPOCO(POCOs.BasicSignal poco)
      {
         var basicSignalVM = new BasicSignalViewModel(
            ControlSliderViewModel.FromPOCO(poco.Frequency),
            ControlSliderViewModel.FromPOCO(poco.Volume),
            ControlSliderViewModel.FromPOCO(poco.ZeroCrossingPosition))
         {
            SignalType = poco.Type
         };

         foreach (var am in poco.AMSignals)
         {
            var amVM = FromPOCO(am);
            basicSignalVM.AddAM(amVM, basicSignalVM);
         }
         foreach (var fm in poco.FMSignals)
         {
            var fmVM = FromPOCO(fm);
            basicSignalVM.AddFM(fmVM, basicSignalVM);
         }

         return basicSignalVM;
      }
      public POCOs.BasicSignal ToPOCO() =>
         new POCOs.BasicSignal()
         {
            Type = BasicSignal.Type,
            Frequency = FreqControlSliderViewModel.ToPOCO(),
            Volume = VolControlSliderViewModel.ToPOCO(),
            ZeroCrossingPosition = ZCPosControlSliderViewModel.ToPOCO(),
            AMSignals = AMSignalVMs.Select(x => x.ToPOCO()).ToList(),
            FMSignals = FMSignalVMs.Select(x => x.ToPOCO()).ToList()
         };

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

         this.ObservableForProperty(x => x.SignalType, skipInitial: false)
            .Subscribe(_ => BasicSignal.Type = SignalType)
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.Frequency, skipInitial: false)
            .Subscribe(_ =>
            {
               FreqControlSliderViewModel.Value = Frequency;
               BasicSignal.Frequency = Frequency;
            })
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.Volume, skipInitial: false)
            .Subscribe(_ =>
            {
               VolControlSliderViewModel.Value = Volume;
               BasicSignal.Gain = Volume;
            })
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.ZeroCrossingPosition, skipInitial: false)
            .Subscribe(_ =>
            {
               ZCPosControlSliderViewModel.Value = ZeroCrossingPosition;
               BasicSignal.ZeroCrossingPosition = ZeroCrossingPosition;
            })
            .DisposeWith(Disposables);

         SignalType = BasicSignalType.Sin;

         AMSignalVMsSourceList = new SourceList<BasicSignalViewModel>().DisposeWith(Disposables);
         AMSignalVMsSourceList.Connect()
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

         FMSignalVMsSourceList = new SourceList<BasicSignalViewModel>().DisposeWith(Disposables);
         FMSignalVMsSourceList.Connect()
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
         this.WhenAnyValue(x => x.Name)
            .Subscribe(_ =>
            {
               //Update name 
               foreach (var am in AMSignalVMsSourceList.Items)
               {
                  am.SetName(AMName, AMSignalVMsSourceList);
               }
               foreach (var fm in FMSignalVMsSourceList.Items)
               {
                  fm.SetName(FMName, FMSignalVMsSourceList);
               }
            })
            .DisposeWith(Disposables);

         DeepSourceListTracker =
            new DeepSourceListTracker<BasicSignalViewModel>(AMSignalVMsSourceList, FMSignalVMsSourceList)
            .DisposeWith(Disposables);

         // HACK Expander IsExpanded is set somewhere from internal avalonia uncontrollable
         this.WhenAnyValue(x => x.IsAMExpanded)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Sample(TimeSpan.FromMilliseconds(30), RxApp.TaskpoolScheduler)
            .Where(x => x).Take(1)
            .SubscribeOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => IsAMExpanded = AMSignalVMs.Count > 0)
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsFMExpanded)
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Sample(TimeSpan.FromMilliseconds(30), RxApp.TaskpoolScheduler)
            .Where(x => x).Take(1)
            .SubscribeOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => IsFMExpanded = FMSignalVMs.Count > 0)
            .DisposeWith(Disposables);
      }

      public ReadOnlyObservableCollection<BasicSignalViewModel> AllLinkableBasicSignalVMs
         => RootSignalTree?.AllSubBasicSignalVMs ??
            new ReadOnlyObservableCollection<BasicSignalViewModel>(
            new ObservableCollection<BasicSignalViewModel>());
      private TrackViewModel rootSignalTree;
      private TrackViewModel RootSignalTree
      {
         get
         {
            if (Parent == null) return null;
            if (rootSignalTree == null)
            {
               var p = Parent;
               while (!(p is TrackViewModel)) p = p.Parent;
               rootSignalTree = p as TrackViewModel;
            }
            return rootSignalTree;
         }
      }

      public void AddAM() => AddAM(CreateAMVM(), this);
      public Task AddAMFromClipboard() => AMSignalVMsSourceList.AddFromClipboard(this, AMName);
      public void RemoveAM(BasicSignalViewModel vm) => AMSignalVMsSourceList.Remove(vm);
      private void AddAM(BasicSignalViewModel vm, ISignalTree parent)
      {
         vm.Parent = parent;
         AMSignalVMsSourceList.Add(vm);
      }


      public void AddFM() => AddFM(CreateFMVM(), this);
      public Task AddFMFromClipboard() => FMSignalVMsSourceList.AddFromClipboard(this, FMName);
      public void RemoveFM(BasicSignalViewModel vm) => FMSignalVMsSourceList.Remove(vm);
      private void AddFM(BasicSignalViewModel vm, ISignalTree parent)
      {
         vm.Parent = parent;
         FMSignalVMsSourceList.Add(vm);
      }

      private static string GetAMName(string name) => $"{name}.AMSignal";
      private static string GetFMName(string name) => $"{name}.FMSignal";
      private string AMName => GetAMName(Name);
      private string FMName => GetFMName(Name);
      private BasicSignalViewModel CreateAMVM() =>
         new BasicSignalViewModel(
            ControlSliderViewModel.ModulationSignalFreq) { Volume = 0 }
         .SetName(AMName, AMSignalVMsSourceList)
         .DisposeWith(Disposables);

      private BasicSignalViewModel CreateFMVM() =>
         new BasicSignalViewModel(
            ControlSliderViewModel.ModulationSignalFreq,
            new ControlSliderViewModel(0, 0, 100, 1, 1, 5))
         { Volume = 0 }
         .SetName(FMName, FMSignalVMsSourceList)
         .DisposeWith(Disposables);

      public async Task CopyToClipboard()
      {
         var poco = this.ToPOCO();
         var json = JsonSerializer.Serialize(poco, new JsonSerializerOptions { WriteIndented = true });
         await Avalonia.Application.Current.Clipboard.SetTextAsync(json);
      }

      public static async Task<BasicSignalViewModel> PasteFromClipboard()
      {
         var json = await Avalonia.Application.Current.Clipboard.GetTextAsync();
         if (string.IsNullOrWhiteSpace(json)) return null;
         try
         {
            var poco = JsonSerializer.Deserialize<POCOs.BasicSignal>(json);
            if (typeof(POCOs.BasicSignal).GetProperties().All(x => x.GetValue(poco).IsNullOrDefault())) return null;
            return BasicSignalViewModel.FromPOCO(poco);
         }
         catch (JsonException)
         {
            return null;
         }
         catch (Exception) { throw; };
      }

      private static readonly Random rand = new Random();
      private Brush GetRandomBrush()
      {
         var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
         return new SolidColorBrush(Color.FromArgb(60, r, g, b));
      }

   }
}
