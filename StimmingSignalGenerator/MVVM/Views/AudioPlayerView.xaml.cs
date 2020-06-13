using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StimmingSignalGenerator.MVVM.Views
{
   public class AudioPlayerView : UserControl
   {
      public AudioPlayerView()
      {
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}