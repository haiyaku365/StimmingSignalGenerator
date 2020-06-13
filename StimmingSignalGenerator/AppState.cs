﻿using ReactiveUI;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.NAudio;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace StimmingSignalGenerator
{
   public class AppState : ReactiveObject, IDisposable
   {
      public bool IsPlaying { get => isPlaying; set => this.RaiseAndSetIfChanged(ref isPlaying, value); }
      public bool IsHideZeroModulation { get => isHideZeroModulation; set => this.RaiseAndSetIfChanged(ref isHideZeroModulation, value); }
      public OSPlatform OSPlatform { get => _OSPlatform; private set => this.RaiseAndSetIfChanged(ref _OSPlatform, value); }
      public string Version =>
         Assembly.GetEntryAssembly()
         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
         .InformationalVersion;

      public AppState()
      {
         IsHideZeroModulation = ConfigurationHelper.GetConfigOrDefault(nameof(IsHideZeroModulation), false);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(nameof(IsHideZeroModulation), () => IsHideZeroModulation.ToString())
            .DisposeWith(Disposables);

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

      private bool isPlaying;
      private bool isHideZeroModulation;
      private OSPlatform _OSPlatform;


      protected CompositeDisposable Disposables { get; } = new CompositeDisposable();
      private bool disposedValue;
      protected virtual void Dispose(bool disposing)
      {
         if (!disposedValue)
         {
            if (disposing)
            {
               // dispose managed state (managed objects)
               Disposables?.Dispose();
            }

            // free unmanaged resources (unmanaged objects) and override finalizer
            // set large fields to null
            disposedValue = true;
         }
      }

      // // override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
      // ~PlotSampleViewModel()
      // {
      //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
      //     Dispose(disposing: false);
      // }

      public void Dispose()
      {
         // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
         Dispose(disposing: true);
         GC.SuppressFinalize(this);
      }
   }
}
