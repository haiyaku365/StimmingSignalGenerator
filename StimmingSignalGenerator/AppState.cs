using ReactiveUI;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace StimmingSignalGenerator
{
   public class AppState : ReactiveObject
   {
      public bool IsHDPlot { get => isHDPlot; set => this.RaiseAndSetIfChanged(ref isHDPlot, value); }
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }
      public bool IsHideZeroModulation { get => isHideZeroModulation; set => this.RaiseAndSetIfChanged(ref isHideZeroModulation, value); }
      public OSPlatform OSPlatform { get => _OSPlatform; private set => this.RaiseAndSetIfChanged(ref _OSPlatform, value); }
      public string Version =>
         Assembly.GetEntryAssembly()
         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         .InformationalVersion;

      public AppState()
      {
         var platforms = new[] { OSPlatform.Windows, OSPlatform.Linux, OSPlatform.FreeBSD, OSPlatform.OSX };
         foreach (var platform in platforms)
         {
            if (RuntimeInformation.IsOSPlatform(platform))
            {
               OSPlatform = platform;
               break;
            }
         }
      }

      private bool isHDPlot;
      private bool isPlotEnable;
      private bool isPlaying;
      private bool isHideZeroModulation;
      private OSPlatform _OSPlatform;
   }
}
