using Avalonia.Media;
using DynamicData;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
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
   public class BasicSignalViewModel : ViewModelBase, ISourceCacheViewModel, IDisposable
   {
      public int Id { get; internal set; }
      int ISourceCacheViewModel.Id { get => Id; set => Id = value; }

      private string name = "SignalGenerator";
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public BasicSignal BasicSignal { get; }
      public ControlSliderViewModel FreqControlSliderViewModel { get; }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ControlSliderViewModel ZCPosControlSliderViewModel { get; }
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
      public bool IsExpanded { get => isExpanded; set => this.RaiseAndSetIfChanged(ref isExpanded, value); }
      public bool IsAMExpanded { get => isAMExpanded; set => this.RaiseAndSetIfChanged(ref isAMExpanded, value); }
      public bool IsFMExpanded { get => isFMExpanded; set => this.RaiseAndSetIfChanged(ref isFMExpanded, value); }

      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> amSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> AMSignalVMs => amSignalVMs;
      private SourceCache<BasicSignalViewModel, int> AMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddAMCommand { get; }
      public ReactiveCommand<Unit, Unit> AddAMFromClipboardCommand { get; }
      public ReactiveCommand<BasicSignalViewModel, Unit> RemoveAMCommand { get; }


      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> fmSignalVMs;
      public ReadOnlyObservableCollection<BasicSignalViewModel> FMSignalVMs => fmSignalVMs;
      private SourceCache<BasicSignalViewModel, int> FMSignalVMsSourceCache { get; }
      public ReactiveCommand<Unit, Unit> AddFMCommand { get; }
      public ReactiveCommand<Unit, Unit> AddFMFromClipboardCommand { get; }
      public ReactiveCommand<BasicSignalViewModel, Unit> RemoveFMCommand { get; }

      public Brush BGColor { get; }

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
            var amVM = FromPOCO(am).SetNameAndId(AMName, basicSignalVM.AMSignalVMsSourceCache);
            basicSignalVM.AddAM(amVM);
         }
         foreach (var fm in poco.FMSignals)
         {
            var fmVM = FromPOCO(fm).SetNameAndId(FMName, basicSignalVM.FMSignalVMsSourceCache);
            basicSignalVM.AddFM(fmVM);
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
         AddAMCommand = ReactiveCommand
            .Create(AddAM)
            .DisposeWith(Disposables);
         AddAMFromClipboardCommand = ReactiveCommand
            .CreateFromTask(AddAMFromClipboard)
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
         AddFMCommand = ReactiveCommand
            .Create(AddFM)
            .DisposeWith(Disposables);
         AddFMFromClipboardCommand = ReactiveCommand
            .CreateFromTask(AddFMFromClipboard)
            .DisposeWith(Disposables);
         RemoveFMCommand = ReactiveCommand.Create<BasicSignalViewModel>(
            vm => FMSignalVMsSourceCache.Remove(vm))
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

      private Task AddAMFromClipboard() => AMSignalVMsSourceCache.AddFromClipboard(AMName);
      private void AddAM() => AddAM(CreateAMVM());
      private void AddAM(BasicSignalViewModel vm) => vm.AddTo(AMSignalVMsSourceCache);

      private Task AddFMFromClipboard() => FMSignalVMsSourceCache.AddFromClipboard(FMName);
      private void AddFM() => AddFM(CreateFMVM());
      private void AddFM(BasicSignalViewModel vm) => vm.AddTo(FMSignalVMsSourceCache);

      private const string AMName = "AMSignal";
      private const string FMName = "FMSignal";

      private BasicSignalViewModel CreateAMVM() =>
            new BasicSignalViewModel(ControlSliderViewModel.ModulationSignalFreq) { Volume = 0 }
            .SetNameAndId(AMName, AMSignalVMsSourceCache)
         .DisposeWith(Disposables);

      private BasicSignalViewModel CreateFMVM() =>
            new BasicSignalViewModel(
               ControlSliderViewModel.ModulationSignalFreq,
               new ControlSliderViewModel(0, 0, 100, 1, 1, 5))
            { Volume = 0 }
            .SetNameAndId(FMName, FMSignalVMsSourceCache)
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
            return BasicSignalViewModel.FromPOCO(poco);
         }
         catch (JsonException)
         {
            return null;
         }
      }

      private BasicSignalType signalType;
      private bool isExpanded;
      private bool isAMExpanded;
      private bool isFMExpanded;

      private static readonly Random rand = new Random();
      private Brush GetRandomBrush()
      {
         var (r, g, b) = Helper.ColorHelper.HsvToRgb(rand.Next(0, 360), 1, 1);
         return new SolidColorBrush(Color.FromArgb(60, r, g, b));
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
