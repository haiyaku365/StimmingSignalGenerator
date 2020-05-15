using ReactiveUI;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator
{
   class AppState : ReactiveObject
   {
      public GeneratorModeType GeneratorMode { get => generatorMode; set => this.RaiseAndSetIfChanged(ref generatorMode, value); }
      public bool IsHDPlot { get => isHDPlot; set => this.RaiseAndSetIfChanged(ref isHDPlot, value); }
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }

      private GeneratorModeType generatorMode;
      private bool isHDPlot;
      private bool isPlotEnable;
      private bool isPlaying;
   }
}
