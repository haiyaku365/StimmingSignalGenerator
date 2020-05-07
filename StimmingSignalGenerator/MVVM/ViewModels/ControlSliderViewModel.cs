using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;


namespace StimmingSignalGenerator.MVVM.ViewModels
{
   class ControlSliderViewModel : ViewModelBase
   {
      private double _value;
      private double minValue;
      private double maxValue;
      private double tickFrequency;
      private double smallChange;
      private double largeChange;


      public const double BasicSignalFreqMin = 300;
      public static ControlSliderViewModel BasicSignalFreq =>
         new ControlSliderViewModel(440, BasicSignalFreqMin, 8000, 1, 10, 50);
      public static ControlSliderViewModel ModulationSignalFreq =>
         new ControlSliderViewModel(1, 0, 6, 0.01, 0.01, 0.05);
      public static ControlSliderViewModel BasicVol => Vol();
      public static ControlSliderViewModel Vol(double initValue = 1) =>
         new ControlSliderViewModel(initValue, 0, 1, 0.0001, 0.0001, 0.005);


      public ControlSliderViewModel() : this(440, 0, 10000, 1, 10, 50) { }
      public ControlSliderViewModel(double value, double minValue, double maxValue, double tickFrequency, double smallChange, double largeChange)
      {
         Value = value;
         MinValue = minValue;
         MaxValue = maxValue;
         TickFrequency = tickFrequency;
         SmallChange = smallChange;
         LargeChange = largeChange;
      }

      public double Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, value); }
      public double MinValue { get => minValue; set => this.RaiseAndSetIfChanged(ref minValue, value); }
      public double MaxValue { get => maxValue; set => this.RaiseAndSetIfChanged(ref maxValue, value); }

      public double TickFrequency { get => tickFrequency; set => this.RaiseAndSetIfChanged(ref tickFrequency, value); }
      public double SmallChange { get => smallChange; set => this.RaiseAndSetIfChanged(ref smallChange, value); }
      public double LargeChange { get => largeChange; set => this.RaiseAndSetIfChanged(ref largeChange, value); }
   }
}
