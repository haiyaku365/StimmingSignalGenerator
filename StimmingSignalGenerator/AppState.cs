using ReactiveUI;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace StimmingSignalGenerator
{
   public class AppState : ReactiveObject
   {
      public bool IsHDPlot { get => isHDPlot; set => this.RaiseAndSetIfChanged(ref isHDPlot, value); }
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }
      public bool IsHideZeroModulation { get => isHideZeroModulation; set => this.RaiseAndSetIfChanged(ref isHideZeroModulation, value); }
      public string Version =>
         Assembly.GetEntryAssembly()
         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         .InformationalVersion;

      private bool isHDPlot;
      private bool isPlotEnable;
      private bool isPlaying;
      private bool isHideZeroModulation;
   }
}
