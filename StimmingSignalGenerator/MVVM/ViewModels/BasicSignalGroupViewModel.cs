using Avalonia.Media;
using DynamicData;
using DynamicData.Binding;
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
   public class DesignBasicSignalGroupViewModel : DesignViewModelBase
   {
      public static BasicSignalGroupViewModel Data
      {
         get
         {
            var track = new TrackViewModel();
            var mod = new BasicSignalGroupViewModel(
               track,
               "DesignSignalGroup", _ => { }, _ => { }, () => new BasicSignalViewModel(track) { IsExpanded = true })
            {
               IsExpanded = true
            };
            mod.Add();
            return mod;
         }
      }
   }
   public class BasicSignalGroupViewModel : ViewModelBase
   {
      public string Name { get; }
      public string Header => header.Value;
      public bool IsExpanded { get => isExpanded; set => this.RaiseAndSetIfChanged(ref isExpanded, value); }
      public ReadOnlyObservableCollection<BasicSignalViewModel> SignalVMs => signalVMs;
      public IObservableList<BasicSignalViewModel> SignalVMsObservableList
         => SignalVMsSourceList.AsObservableList();
      public ISignalTree Parent { get; }

      private readonly ObservableAsPropertyHelper<string> header;
      private bool isExpanded;
      private SourceList<BasicSignalViewModel> SignalVMsSourceList { get; }
      private readonly ReadOnlyObservableCollection<BasicSignalViewModel> signalVMs;
      private Func<BasicSignalViewModel> CreateVM { get; }

      public BasicSignalGroupViewModel(
         ISignalTree parent,
         string name,
         Action<BasicSignalViewModel> onSignalVmAdded,
         Action<BasicSignalViewModel> onSignalVmRemoved,
         Func<BasicSignalViewModel> createVM)
      {
         Parent = parent;
         Name = name;
         CreateVM = createVM;

         SignalVMsSourceList = new SourceList<BasicSignalViewModel>().DisposeWith(Disposables);
         SignalVMsSourceList.Connect()
            .OnItemAdded(vm => onSignalVmAdded(vm))
            .OnItemRemoved(vm =>
            {
               onSignalVmRemoved(vm);
               vm.Dispose();
            })
            .ObserveOn(RxApp.MainThreadScheduler) // Make sure this is only right before the Bind()
            .Bind(out signalVMs)
            .Subscribe()
            .DisposeWith(Disposables);

         SignalVMsObservableList.CountChanged
            .Select(_ => $"{SignalVMs.Count} {Name} signal(s)")
            .ToProperty(this, nameof(Header), out header)
            .DisposeWith(Disposables);
      }

      public void CollapseIfEmpty() => IsExpanded = SignalVMs.Count > 0;

      public void Add() => Add(CreateVM().DisposeWith(Disposables));
      public void Add(BasicSignalViewModel vm) =>
         vm.AddAndSetName(Name, SignalVMsSourceList);
      public Task AddFromClipboard() =>
         SignalVMsSourceList.AddFromClipboard(Parent, Name, Disposables);
      public void Remove(BasicSignalViewModel vm) =>
         vm.RemoveAndMaintainName(Name, SignalVMsSourceList);
   }
}
