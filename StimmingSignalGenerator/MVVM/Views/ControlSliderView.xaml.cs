using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System.Collections;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class ControlSliderView : UserControl
   {
      public ControlSliderView()
      {
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}