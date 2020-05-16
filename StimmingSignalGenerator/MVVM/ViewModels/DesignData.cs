using Splat;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public static class DesignData
   {
      public static ControlSliderViewModel ControlSliderViewModel
      {
         get
         {
            var isFreq = RandomBool(50);
            var min = isFreq ? rand.Next(0, 1000) : 0;
            var max = isFreq ? rand.Next(5000, 7000) : 1;
            var value = isFreq ? rand.Next(min, max) : rand.NextDouble();
            return new ControlSliderViewModel()
            {
               MinValue = min,
               MaxValue = max,
               Value = value
            };
         }
      }
      public static BasicSignalViewModel BasicSignalViewModel =>
         new BasicSignalViewModel
         {
            Name = $"Signal{rand.Next(0, 100)}",
            SignalType = GetRandomEnum<BasicSignalType>(),
            Frequency = rand.Next(300, 7000),
            Volume = rand.NextDouble(),
            ZeroCrossingPosition = rand.NextDouble(),
            IsExpanded = true
         };

      public static MultiSignalViewModel MultiSignalViewModel =>
         new MultiSignalViewModel();
      public static AudioPlayerViewModel AudioPlayerViewModel =>
         new AudioPlayerViewModel(new BasicSignal());

      public static PlotSampleViewModel PlotSampleViewModel =>
         new PlotSampleViewModel(new PlotSampleProvider(new BasicSignal()));

      public static PlotViewModel MonoPlotViewModel => CreatePlotViewModel(GeneratorModeType.Mono);
      public static PlotViewModel StereoPlotViewModel => CreatePlotViewModel(GeneratorModeType.Stereo);
      static PlotViewModel CreatePlotViewModel(GeneratorModeType generatorModeType)
      {
         Locator.CurrentMutable.RegisterConstant(CreateAppState(generatorModeType));
         return new PlotViewModel(Enumerable.Repeat(new MultiSignalViewModel(), 3).ToList());
      }

      public static PresetViewModel MonoPresetViewModel => CreatePresetViewModel(GeneratorModeType.Mono);
      public static PresetViewModel StereoPresetViewModel => CreatePresetViewModel(GeneratorModeType.Stereo);
      static PresetViewModel CreatePresetViewModel(GeneratorModeType generatorModeType)
      {
         Locator.CurrentMutable.RegisterConstant(CreateAppState(generatorModeType));
         return new PresetViewModel();
      }

      public static MainWindowViewModel MonoMainWindowViewModel => CreateMainWindowViewModel(GeneratorModeType.Mono);
      public static MainWindowViewModel StereoMainWindowViewModel => CreateMainWindowViewModel(GeneratorModeType.Stereo);
      static MainWindowViewModel CreateMainWindowViewModel(GeneratorModeType generatorModeType)
      {
         Locator.CurrentMutable.RegisterConstant(CreateAppState(generatorModeType));
         return new MainWindowViewModel();
      }
      static AppState CreateAppState(GeneratorModeType generatorModeType) => new AppState
      {
         GeneratorMode = generatorModeType,
         IsPlotEnable = true
      };

      static readonly Random rand = new Random();
      static bool RandomBool(int percentChange) => rand.Next(0, 100) < percentChange;
      static T GetRandomEnum<T>() where T : Enum => GetEnumValues<T>().GetRandom();
      static T GetRandom<T>(this T[] array) => array[rand.Next(0, array.Length)];
      static T[] GetEnumValues<T>() where T : Enum => (T[])Enum.GetValues(typeof(T));
   }
}
