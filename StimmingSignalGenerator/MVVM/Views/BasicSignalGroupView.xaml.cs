using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.ViewModels;
using StimmingSignalGenerator.MVVM.UiHelper;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class BasicSignalGroupView : ReactiveUserControlEx<BasicSignalGroupViewModel>
   {
      public BasicSignalGroupView()
      {
         this.WhenActivated(disposables =>
         {
            //initial IsExpanded
            SafeViewModel(vm => vm.CollapseIfEmpty());
         });
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}