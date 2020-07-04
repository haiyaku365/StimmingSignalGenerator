using Avalonia.Media;
using DynamicData;
using ReactiveUI;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.MVVM.UiHelper;
using StimmingSignalGenerator.MVVM.ViewModels.Interface;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignBasicSignalViewModel : DesignViewModelBase
   {
      public static BasicSignalViewModel Data
      {
         get
         {
            var track = new TrackViewModel();
            return new BasicSignalViewModel(track.MultiSignalVMs[0])
            {
               Name = $"Signal{random.Next(0, 100)}",
               SignalType = GetRandomEnum<BasicSignalType>(),
               Frequency = random.Next(300, 7000),
               Volume = random.NextDouble(),
               ZeroCrossingPosition = random.NextDouble(),
               IsExpanded = true
            };
         }
      }
   }
   public class BasicSignalViewModel : ViewModelBase,
      ISignalTree, IDeepSourceList<BasicSignalViewModel>
   {
      public string Name { get => name; set => this.RaiseAndSetIfChanged(ref name, value); }
      public string FullName => fullName.Value;
      public Brush BGColor { get; }
      public BasicSignal BasicSignal { get; }
      public ControlSliderViewModel FreqControlSliderViewModel { get; }
      public ControlSliderViewModel PhaseShiftControlSliderViewModel { get; }
      public ControlSliderViewModel VolControlSliderViewModel { get; }
      public ControlSliderViewModel ZCPosControlSliderViewModel { get; }

      public BasicSignalType SignalType { get => signalType; set { this.RaiseAndSetIfChanged(ref signalType, value); } }
      public double Frequency { get => frequency; set { this.RaiseAndSetIfChanged(ref frequency, value); } }
      public double PhaseShift { get => phaseShift; set { this.RaiseAndSetIfChanged(ref phaseShift, value); } }
      public double Volume { get => volume; set { this.RaiseAndSetIfChanged(ref volume, value); } }
      public double ZeroCrossingPosition { get => zeroCrossingPosition; set { this.RaiseAndSetIfChanged(ref zeroCrossingPosition, value); } }

      public bool IsExpanded { get => isExpanded; set => this.RaiseAndSetIfChanged(ref isExpanded, value); }

      public BasicSignalGroupViewModel AmplitudeModulationSignalsViewModel { get; }
      public BasicSignalGroupViewModel FrequencyModulationSignalsViewModel { get; }
      public BasicSignalGroupViewModel PhaseModulationSignalsViewModel { get; }
      public BasicSignalGroupViewModel ZCPModulationSignalsViewModel { get; }

      public ISignalTree Parent { get; }
      public IObservable<BasicSignalViewModel> ObservableItemAdded => DeepSourceListTracker.ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableItemRemoved => DeepSourceListTracker.ObservableItemRemoved;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsAdded => ObservableItemAdded;
      public IObservable<BasicSignalViewModel> ObservableBasicSignalViewModelsRemoved => ObservableItemRemoved;

      public BasicSignalViewModel SelectedLinkableBasicSignalVM { get => selectedLinkableBasicSignalVM; set => this.RaiseAndSetIfChanged(ref selectedLinkableBasicSignalVM, value); }
      public bool IsSyncFreq { get => isSyncFreq; set => this.RaiseAndSetIfChanged(ref isSyncFreq, value); }
      public bool CanSyncFreq => canSyncFreq.Value;
      public ReadOnlyObservableCollection<BasicSignalViewModel> AllLinkableBasicSignalVMs => allLinkableBasicSignalVMs;

      private string name = string.Empty;
      private readonly ObservableAsPropertyHelper<string> fullName;
      private BasicSignalType signalType;
      private double frequency;
      private double phaseShift;
      private double volume;
      private double zeroCrossingPosition;
      private bool isExpanded;
      private DeepSourceListTracker<BasicSignalViewModel> DeepSourceListTracker { get; }
      private TrackViewModel RootSignalTree
      {
         get
         {
            if (rootSignalTree == null)
            {
               var p = Parent;
               while (!(p is TrackViewModel)) p = p.Parent;
               rootSignalTree = p as TrackViewModel;
            }
            return rootSignalTree;
         }
      }

      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> allLinkableBasicSignalVMs;
      private BasicSignalViewModel selectedLinkableBasicSignalVM;
      private bool isSyncFreq;
      private readonly ObservableAsPropertyHelper<bool> canSyncFreq;
      private TrackViewModel rootSignalTree;

      public static BasicSignalViewModel FromPOCO(POCOs.BasicSignal poco, ISignalTree parent)
      {
         var basicSignalVM = new BasicSignalViewModel(
            parent,
            ControlSliderViewModel.FromPOCO(poco.Frequency),
            // default for backward compatibility with V0.2 save file 
            ControlSliderViewModel.FromPOCOorDefault(poco.PhaseShift, ControlSliderViewModel.Vol(0)),
            ControlSliderViewModel.FromPOCO(poco.Volume),
            ControlSliderViewModel.FromPOCO(poco.ZeroCrossingPosition))
         {
            SignalType = poco.Type
         };

         //init freq sync
         if (!string.IsNullOrWhiteSpace(poco.FrequencySyncFrom))
         {
            basicSignalVM.RootSignalTree.AllSubBasicSignalVMs
               .Connect()
               .Filter(x => x.FullName == poco.FrequencySyncFrom)
               .Timeout(TimeSpan.FromMinutes(1)) //cancel in 1 min if not find any
               .Take(1)
               .ToCollection()
               .Subscribe(
                  onNext: x =>
                  {
                     basicSignalVM.IsSyncFreq = true;
                     basicSignalVM.SelectedLinkableBasicSignalVM = x.First();
                  },
                  onError: x =>
                  {
                     if (x is TimeoutException) return;
                     throw x;
                  });
         }

         foreach (var am in poco.AMSignals)
         {
            var vm = FromPOCO(am, basicSignalVM);
            basicSignalVM.AmplitudeModulationSignalsViewModel.Add(vm);
         }
         foreach (var fm in poco.FMSignals)
         {
            var vm = FromPOCO(fm, basicSignalVM);
            basicSignalVM.FrequencyModulationSignalsViewModel.Add(vm);
         }
         // default for backward compatibility with V0.2 save file 
         foreach (var pm in poco.PMSignals ?? new List<POCOs.BasicSignal>())
         {
            var vm = FromPOCO(pm, basicSignalVM);
            basicSignalVM.PhaseModulationSignalsViewModel.Add(vm);
         }
         // default for backward compatibility with V0.3 save file 
         foreach (var pm in poco.ZMSignals ?? new List<POCOs.BasicSignal>())
         {
            var vm = FromPOCO(pm, basicSignalVM);
            basicSignalVM.ZCPModulationSignalsViewModel.Add(vm);
         }

         return basicSignalVM;
      }
      public POCOs.BasicSignal ToPOCO() =>
         new POCOs.BasicSignal()
         {
            Type = BasicSignal.Type,
            Frequency = FreqControlSliderViewModel.ToPOCO(),
            PhaseShift = PhaseShiftControlSliderViewModel.ToPOCO(),
            Volume = VolControlSliderViewModel.ToPOCO(),
            ZeroCrossingPosition = ZCPosControlSliderViewModel.ToPOCO(),
            AMSignals = AmplitudeModulationSignalsViewModel.SignalVMs.Select(x => x.ToPOCO()).ToList(),
            FMSignals = FrequencyModulationSignalsViewModel.SignalVMs.Select(x => x.ToPOCO()).ToList(),
            PMSignals = PhaseModulationSignalsViewModel.SignalVMs.Select(x => x.ToPOCO()).ToList(),
            ZMSignals = ZCPModulationSignalsViewModel.SignalVMs.Select(x => x.ToPOCO()).ToList(),
            FrequencySyncFrom = SelectedLinkableBasicSignalVM?.FullName
         };

      public BasicSignalViewModel(ISignalTree parent)
         : this(parent, ControlSliderViewModel.BasicSignalFreq)
      { }
      public BasicSignalViewModel(ISignalTree parent,
         ControlSliderViewModel freqControlSliderViewModel)
         : this(parent, freqControlSliderViewModel, ControlSliderViewModel.BasicVol)
      { }
      public BasicSignalViewModel(ISignalTree parent,
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel)
         : this(parent,
              freqControlSliderViewModel, ControlSliderViewModel.Vol(0),
              volControlSliderViewModel, ControlSliderViewModel.Vol(0.5))
      { }

      public BasicSignalViewModel(ISignalTree parent,
         ControlSliderViewModel freqControlSliderViewModel,
         ControlSliderViewModel phaseShiftControlSliderViewModel,
         ControlSliderViewModel volControlSliderViewModel,
         ControlSliderViewModel zcPosControlSliderViewModel
         )
      {
         Parent = parent ?? throw new ArgumentNullException(nameof(parent));
         BGColor = GetRandomBrush();

         FreqControlSliderViewModel = freqControlSliderViewModel;
         PhaseShiftControlSliderViewModel = phaseShiftControlSliderViewModel;
         VolControlSliderViewModel = volControlSliderViewModel;
         ZCPosControlSliderViewModel = zcPosControlSliderViewModel;

         BasicSignal = new BasicSignal(
            VolControlSliderViewModel.Value,
            FreqControlSliderViewModel.Value);
         SignalType = BasicSignalType.Sin;

         #region Prop bind
         // bind control slider to prop
         FreqControlSliderViewModel
            .WhenAnyValue(x => x.Value)
            .Subscribe(x => Frequency = x)
            .DisposeWith(Disposables);
         PhaseShiftControlSliderViewModel
            .WhenAnyValue(x => x.Value)
            .Subscribe(x => PhaseShift = x)
            .DisposeWith(Disposables);
         VolControlSliderViewModel
            .WhenAnyValue(x => x.Value)
            .Subscribe(x => Volume = x)
            .DisposeWith(Disposables);
         ZCPosControlSliderViewModel
            .WhenAnyValue(x => x.Value)
            .Subscribe(x => ZeroCrossingPosition = x)
            .DisposeWith(Disposables);

         // bind prop to control slider and generator
         this.WhenAnyValue(x => x.SignalType)
            .Subscribe(_ => BasicSignal.Type = SignalType)
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.Frequency)
            .Subscribe(_ =>
            {
               FreqControlSliderViewModel.Value = Frequency;
               BasicSignal.Frequency = Frequency;
            })
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.PhaseShift)
            .Subscribe(_ =>
            {
               PhaseShiftControlSliderViewModel.Value = PhaseShift;
               BasicSignal.PhaseShift = PhaseShift;
            })
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.Volume)
            .Subscribe(_ =>
            {
               VolControlSliderViewModel.Value = Volume;
               BasicSignal.Gain = Volume;
            })
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.ZeroCrossingPosition)
            .Subscribe(_ =>
            {
               ZCPosControlSliderViewModel.Value = ZeroCrossingPosition;
               BasicSignal.ZeroCrossingPosition = ZeroCrossingPosition;
            })
            .DisposeWith(Disposables);
         #endregion

         #region Setup modulation vm
         AmplitudeModulationSignalsViewModel =
            new BasicSignalGroupViewModel(
               parent: this,
               name: Constants.ViewModelName.AMName,
               onSignalVmAdded: vm => BasicSignal.AddAMSignal(vm.BasicSignal),
               onSignalVmRemoved: vm => BasicSignal.RemoveAMSignal(vm.BasicSignal),
               createVM: () =>
                  new BasicSignalViewModel(this,
                     ControlSliderViewModel.ModulationSignalFreq,
                     ControlSliderViewModel.Vol(0))
                  { IsExpanded = true }
            ).DisposeWith(Disposables);
         FrequencyModulationSignalsViewModel =
            new BasicSignalGroupViewModel(
               parent: this,
               name: Constants.ViewModelName.FMName,
               onSignalVmAdded: vm => BasicSignal.AddFMSignal(vm.BasicSignal),
               onSignalVmRemoved: vm => BasicSignal.RemoveFMSignal(vm.BasicSignal),
               createVM: () =>
                   new BasicSignalViewModel(this,
                      ControlSliderViewModel.ModulationSignalFreq,
                      new ControlSliderViewModel(0, 0, 100, 1, 1, 5))
                   { IsExpanded = true }
               ).DisposeWith(Disposables);
         PhaseModulationSignalsViewModel =
            new BasicSignalGroupViewModel(
               parent: this,
               name: Constants.ViewModelName.PMName,
               onSignalVmAdded: vm => BasicSignal.AddPMSignal(vm.BasicSignal),
               onSignalVmRemoved: vm => BasicSignal.RemovePMSignal(vm.BasicSignal),
               createVM: () =>
                   new BasicSignalViewModel(this,
                      ControlSliderViewModel.ModulationSignalFreq,
                      ControlSliderViewModel.Vol(0))
                   { IsExpanded = true }
               ).DisposeWith(Disposables);
         ZCPModulationSignalsViewModel =
            new BasicSignalGroupViewModel(
               parent: this,
               name: Constants.ViewModelName.ZMName,
               onSignalVmAdded: vm => BasicSignal.AddZMSignal(vm.BasicSignal),
               onSignalVmRemoved: vm => BasicSignal.RemoveZMSignal(vm.BasicSignal),
               createVM: () =>
                   new BasicSignalViewModel(this,
                      ControlSliderViewModel.ModulationSignalFreq,
                      ControlSliderViewModel.Vol(0))
                   { IsExpanded = true }
               ).DisposeWith(Disposables);
         #endregion

         DeepSourceListTracker =
            new DeepSourceListTracker<BasicSignalViewModel>(
               AmplitudeModulationSignalsViewModel.SignalVMsObservableList,
               FrequencyModulationSignalsViewModel.SignalVMsObservableList,
               PhaseModulationSignalsViewModel.SignalVMsObservableList,
               ZCPModulationSignalsViewModel.SignalVMsObservableList)
            .DisposeWith(Disposables);

         var RootSignalTreeAllSubBasicSignalVMs =
            RootSignalTree.AllSubBasicSignalVMs.Connect().Publish();

         //Will not sync with self and signal that sync to another
         RootSignalTreeAllSubBasicSignalVMs
            .AutoRefresh(x => x.IsSyncFreq)
            .Filter(x => x != this && !x.IsSyncFreq)
            .Bind(out allLinkableBasicSignalVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         //Not allow to sync if there are some signal sync to this one
         RootSignalTreeAllSubBasicSignalVMs
            .AutoRefresh(x => x.SelectedLinkableBasicSignalVM)
            .Filter(x => x.SelectedLinkableBasicSignalVM == this)
            .ToCollection()
            .Select(x => x.Count == 0)
            .ToProperty(this, nameof(CanSyncFreq), out canSyncFreq, initialValue: true)
            .DisposeWith(Disposables);

         RootSignalTreeAllSubBasicSignalVMs.Connect().DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsSyncFreq)
            .Subscribe(_ => { if (!IsSyncFreq) { SelectedLinkableBasicSignalVM = null; } })
            .DisposeWith(Disposables);

         this.WhenAnyValue(
            property1: x => x.SelectedLinkableBasicSignalVM,
            property2: x => x.SelectedLinkableBasicSignalVM.Frequency,
            property3: x => x.SelectedLinkableBasicSignalVM.SignalType,
            property4: x => x.SignalType)
            .Subscribe(x =>
            {
               var (vm, f, t1, t2) = x;
               if (vm == null)
               {
                  IsSyncFreq = false;
                  BasicSignal.Frequency = Frequency;

                  // reset phase shift if not sync to any signal
                  // to avoid confusion of having master signal phase shifted
                  PhaseShift = 0;
               }
               else
               {
                  BasicSignal.SetFrequencyAndPhaseTo(SelectedLinkableBasicSignalVM.BasicSignal);
                  if ((t1 == BasicSignalType.White || t1 == BasicSignalType.Pink) && t1 == t2)
                  {
                     SyncRandomTo(SelectedLinkableBasicSignalVM.BasicSignal);
                  }
               }
            })
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.Name, x => x.Parent.FullName)
            .Select(_ => $"{Parent.FullName}.{Name}")
            .ToProperty(this, nameof(FullName), out fullName)
            .DisposeWith(Disposables);
      }

      CancellationTokenSource SyncRandomCancellationTokenSource;
      bool OnreadSubed = false;

      ISampleProviderReadEvent RootSignalTreeFinalSampleReadEvent
         => RootSignalTree.FinalSample as ISampleProviderReadEvent;
      IObservable<EventPattern<object>> OnRootSignalTreeFinalSampleRead
         => Observable.FromEventPattern(
            h => RootSignalTreeFinalSampleReadEvent.OnRead += h,
            h => RootSignalTreeFinalSampleReadEvent.OnRead -= h);
      private void SyncRandomTo(BasicSignal basicSignal)
      {
         if (OnreadSubed) return;
         SyncRandomCancellationTokenSource?.Cancel();
         SyncRandomCancellationTokenSource = new CancellationTokenSource().DisposeWith(Disposables);
         OnRootSignalTreeFinalSampleRead
            .Take(1)
            .Subscribe(
            _ =>
            {
               OnreadSubed = false;
               try
               {
                  Task.Run(() =>
                  {
                     BasicSignal.SyncRandomTo(basicSignal);
                     if (Parent is BasicSignalViewModel parent && parent.SelectedLinkableBasicSignalVM != null)
                     {
                        parent.BasicSignal.SetFrequencyAndPhaseTo(parent.SelectedLinkableBasicSignalVM.BasicSignal);
                     }
                  }, SyncRandomCancellationTokenSource.Token).Wait();
               }
               catch (TaskCanceledException) { }
               catch (AggregateException ae)
               {
                  foreach (var e in ae.Flatten().InnerExceptions)
                  {
                     if (!(e is TaskCanceledException)) throw;
                  }
               }
               catch (Exception) { throw; }
            });
         OnreadSubed = true;
      }

      public async Task CopyToClipboard()
      {
         var poco = this.ToPOCO();
         var json = JsonSerializer.Serialize(poco, new JsonSerializerOptions { WriteIndented = true });
         await Avalonia.Application.Current.Clipboard.SetTextAsync(json);
      }

      public static async Task<BasicSignalViewModel> PasteFromClipboard(ISignalTree parent)
      {
         var json = await Avalonia.Application.Current.Clipboard.GetTextAsync();
         if (string.IsNullOrWhiteSpace(json)) return null;
         try
         {
            var poco = JsonSerializer.Deserialize<POCOs.BasicSignal>(json);
            if (typeof(POCOs.BasicSignal).GetProperties().All(x => x.GetValue(poco).IsNullOrDefault())) return null;
            return BasicSignalViewModel.FromPOCO(poco, parent);
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
