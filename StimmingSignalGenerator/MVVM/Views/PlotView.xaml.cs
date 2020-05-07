using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StimmingSignalGenerator.MVVM.Views
{
    public class PlotView : UserControl
    {
        public PlotView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}