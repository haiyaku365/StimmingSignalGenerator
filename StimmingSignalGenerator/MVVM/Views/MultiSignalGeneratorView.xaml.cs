using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace StimmingSignalGenerator.MVVM.Views
{
    public class MultiSignalGeneratorView : UserControl
    {
        public MultiSignalGeneratorView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}