using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using System.Collections;

namespace StimmingSignalGenerator.MVVM.Controls
{
   public class SignalSlider : UserControl
   {
      public static readonly DirectProperty<SignalSlider, double> ValueProperty =
         AvaloniaProperty.RegisterDirect<SignalSlider, double>( nameof(Value), 
            o => o.Value, (o, v) => o.Value = v,
            defaultBindingMode:BindingMode.TwoWay);
      public static readonly DirectProperty<SignalSlider, double> MinValueProperty =
         AvaloniaProperty.RegisterDirect<SignalSlider, double>(nameof(MinValue), 
            o => o.MinValue, (o, v) => o.MinValue = v, 
            defaultBindingMode: BindingMode.TwoWay);
      public static readonly DirectProperty<SignalSlider, double> MaxValueProperty =
         AvaloniaProperty.RegisterDirect<SignalSlider, double>(nameof(MaxValue), 
            o => o.MaxValue, (o, v) => o.MaxValue = v, 
            defaultBindingMode: BindingMode.TwoWay);

      public static readonly StyledProperty<double> TickFrequencyProperty =
        Slider.TickFrequencyProperty.AddOwner<SignalSlider>();
      public static readonly StyledProperty<double> SmallChangeProperty =
        Slider.SmallChangeProperty.AddOwner<SignalSlider>();
      public static readonly StyledProperty<double> LargeChangeProperty =
        Slider.LargeChangeProperty.AddOwner<SignalSlider>();

      private double _value, minValue, maxValue;
      public double Value { get => _value; set => SetAndRaise(ValueProperty, ref _value, value); }
      public double MinValue { get => minValue; set => SetAndRaise(MinValueProperty, ref minValue, value); }
      public double MaxValue { get => maxValue; set => SetAndRaise(MaxValueProperty, ref maxValue, value); } 

      public double TickFrequency { get => GetValue(TickFrequencyProperty); set => SetValue(TickFrequencyProperty, value); }
      public double SmallChange { get => GetValue(SmallChangeProperty); set => SetValue(SmallChangeProperty, value); }
      public double LargeChange { get => GetValue(LargeChangeProperty); set => SetValue(LargeChangeProperty, value); }

      public SignalSlider()
      {
         InitializeComponent();
      }

      private void InitializeComponent()
      {
         AvaloniaXamlLoader.Load(this);
      }
   }
}