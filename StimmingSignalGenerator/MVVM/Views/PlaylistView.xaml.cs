using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StimmingSignalGenerator.MVVM.Views
{
    public class PlaylistView : UserControl
    {
        public PlaylistView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}