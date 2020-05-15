using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;
using Splat;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   class ControlSliderViewModel : ViewModelBase
   {
      public AppState AppState { get; }
      public double Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, value); }
      public double MinValue { get => minValue; set => this.RaiseAndSetIfChanged(ref minValue, value); }
      public double MaxValue
      {
         get => maxValue; set
         {
            this.RaiseAndSetIfChanged(ref maxValue, value);
            AdjustStepChange();
         }
      }
      public double TickFrequency { get => tickFrequency; set => this.RaiseAndSetIfChanged(ref tickFrequency, value); }
      public double SmallChange { get => smallChange; set => this.RaiseAndSetIfChanged(ref smallChange, value); }
      public double LargeChange { get => largeChange; set => this.RaiseAndSetIfChanged(ref largeChange, value); }
      
      public const double BasicSignalFreqMin = 300;
      public const double Tick = 1;
      public const double SmallTick = 0.01;
      public const double SuperSmallTick = 0.0001;
      public static ControlSliderViewModel BasicSignalFreq =>
         new ControlSliderViewModel(440, BasicSignalFreqMin, 8000, Tick, Tick, Tick);
      public static ControlSliderViewModel ModulationSignalFreq =>
         new ControlSliderViewModel(1, 0, 6, SmallTick, SmallTick, SmallTick);
      public static ControlSliderViewModel BasicVol => Vol();
      public static ControlSliderViewModel Vol(double initValue = 1) =>
         new ControlSliderViewModel(initValue, 0, 1, SuperSmallTick, SuperSmallTick, SuperSmallTick);
      public ControlSliderViewModel() : this(440, 0, 10000, 1, 10, 50) { }
      public ControlSliderViewModel(double value, double minValue, double maxValue, double tickFrequency, double smallChange, double largeChange)
      {
         AppState = Locator.Current.GetService<AppState>();

         Value = value;
         MinValue = minValue;
         MaxValue = maxValue;
         TickFrequency = tickFrequency;
         SmallChange = smallChange;
         LargeChange = largeChange;
      }

      private double _value;
      private double minValue;
      private double maxValue;
      private double tickFrequency;
      private double smallChange;
      private double largeChange;
      private void AdjustStepChange()
      {
         if (MaxValue <= 1)
         {
            TickFrequency = SmallChange = LargeChange = SuperSmallTick;
         }
         else if (MaxValue < 20)
         {
            TickFrequency = SmallChange = LargeChange = SmallTick;
         }
         else
         {
            TickFrequency = SmallChange = LargeChange = Tick;
         }
      }
      public static ControlSliderViewModel FromPOCO(POCOs.ControlSlider poco) 
         => new ControlSliderViewModel().SetToPOCO(poco);
      public ControlSliderViewModel SetToPOCO(POCOs.ControlSlider poco)
      {
         MinValue = poco.Min;
         MaxValue = poco.Max;
         Value = poco.Value;
         return this;
      }
      public POCOs.ControlSlider ToPOCO() =>
         new POCOs.ControlSlider()
         {
            Min = MinValue,
            Max = MaxValue,
            Value = Value
         };
   }
}
