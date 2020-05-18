﻿using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignMainWindowViewModel : DesignViewModelBase
   {
      public static MainWindowViewModel MonoData => CreateMainWindowViewModel(GeneratorModeType.Mono);
      public static MainWindowViewModel StereoData => CreateMainWindowViewModel(GeneratorModeType.Stereo);
      static MainWindowViewModel CreateMainWindowViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState(generatorModeType);
         return new MainWindowViewModel();
      }
   }
   public class MainWindowViewModel : ViewModelBase, IDisposable
   {
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public TrackViewModel TrackViewModel { get; }
      public AppState AppState { get; }
      public string Title => $"Stimming Signal Generator {AppState.Version}";

      public MainWindowViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();
         TrackViewModel = new TrackViewModel();

         AudioPlayerViewModel =
            new AudioPlayerViewModel(TrackViewModel.FinalSample)
            .DisposeWith(Disposables);
      }

      public void OpenGitHubPage()
      {
         System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo()
            {
               FileName = "https://github.com/haiyaku365/StimmingSignalGenerator",
               UseShellExecute = true
            }
            );
      }

      private CompositeDisposable Disposables { get; } = new CompositeDisposable();
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
      // ~MainWindowViewModel()
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
