using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Helper;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignControlSliderViewModel : DesignViewModelBase
   {
      public static ControlSliderViewModel Data
      {
         get
         {
            var isFreq = RandomBool(50);
            var min = isFreq ? random.Next(0, 1000) : 0;
            var max = isFreq ? random.Next(5000, 7000) : 1;
            var value = isFreq ? random.Next(min, max) : random.NextDouble();
            return new ControlSliderViewModel()
            {
               MinValue = min,
               MaxValue = max,
               Value = value
            };
         }
      }
   }
   public class ControlSliderViewModel : ViewModelBase
   {
      public double Value { get => _value; set => this.RaiseAndSetIfChanged(ref _value, Math.Round(value, 4)); }
      public double MinValue
      {
         get => minValue; set
         {
            this.RaiseAndSetIfChanged(ref minValue, Math.Round(value, 4));
            AdjustStepChange();
         }
      }
      public double MaxValue
      {
         get => maxValue; set
         {
            this.RaiseAndSetIfChanged(ref maxValue, Math.Round(value, 4));
            AdjustStepChange();
         }
      }
      public double TickFrequency { get => tickFrequency; set => this.RaiseAndSetIfChanged(ref tickFrequency, value); }
      public double SmallChange { get => smallChange; set => this.RaiseAndSetIfChanged(ref smallChange, value); }
      public double LargeChange { get => largeChange; set => this.RaiseAndSetIfChanged(ref largeChange, value); }
      public string NumericUpDownTextFormat { get => numericUpDownTextFormat; set => this.RaiseAndSetIfChanged(ref numericUpDownTextFormat, value); }

      public const double BasicSignalFreqMin = 300;
      public const double Tick = 1;
      public const double SmallTick = 0.01;
      public const double SuperSmallTick = 0.001;
      public const string TextFormat = "{0:N0}";
      public const string SmallTickTextFormat = "{0:N2}";
      public const string SuperSmallTickTextFormat = "{0:N3}";
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
      private string numericUpDownTextFormat;

      private void AdjustStepChange()
      {
         var span = MaxValue - MinValue;
         if (span <= 1)
         {
            TickFrequency = SmallChange = LargeChange = SuperSmallTick;
            NumericUpDownTextFormat = SuperSmallTickTextFormat;
         }
         else if (span <= 100)
         {
            TickFrequency = SmallChange = LargeChange = SmallTick;
            NumericUpDownTextFormat = SmallTickTextFormat;

         }
         else
         {
            TickFrequency = SmallChange = LargeChange = Tick;
            NumericUpDownTextFormat = TextFormat;

         }
      }
      public async Task CopyToClipboard()
      {
         var poco = this.ToPOCO();
         var json = JsonSerializer.Serialize(poco, new JsonSerializerOptions { WriteIndented = true });
         await Avalonia.Application.Current.Clipboard.SetTextAsync(json);
      }
      public async Task PasteValueFromClipboard()
      {
         var json = await Avalonia.Application.Current.Clipboard.GetTextAsync();
         if (string.IsNullOrWhiteSpace(json)) return;
         try
         {
            var poco = JsonSerializer.Deserialize<POCOs.ControlSlider>(json);
            if (typeof(POCOs.ControlSlider).GetProperties().All(x => x.GetValue(poco).IsNullOrDefault())) return;
            SetToPOCO(poco);
         }
         catch (JsonException)
         {
            return;
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
