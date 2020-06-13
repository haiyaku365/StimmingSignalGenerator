using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class BasicSignalView : UserControl
   {
      public BasicSignalView()
      {
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}