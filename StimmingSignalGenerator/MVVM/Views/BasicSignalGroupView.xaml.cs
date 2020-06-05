using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.UiHelper;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class BasicSignalGroupView : ReactiveUserControlEx<BasicSignalGroupViewModel>
   {
      Button AddButton => this.FindControl<Button>("AddButton");
      public BasicSignalGroupView()
      {
         this.WhenActivated(disposables =>
         {
            SafeViewModel(vm =>
            {
               //initial IsExpanded
               vm.CollapseIfEmpty();

               //setup hide 0 mod
               vm.WhenAnyValue(x => x.AppState.IsHideZeroModulation)
                  .Subscribe(_ =>
                  {
                     if (vm.AppState.IsHideZeroModulation)
                     {
                        this.IsVisible = vm.SignalVMsObservableList.Count > 0;
                     }
                     else
                     {
                        this.IsVisible = true;
                     }
                     AddButton.IsVisible = !vm.AppState.IsHideZeroModulation;
                  })
                  .DisposeWith(disposables);
            });
         });
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}