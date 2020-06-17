﻿using Avalonia;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.NAudio.OxyPlot;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignMainWindowViewModel : DesignViewModelBase
   {
      public static MainWindowViewModel MonoData => CreateMainWindowViewModel(GeneratorModeType.Mono);
      public static MainWindowViewModel StereoData => CreateMainWindowViewModel(GeneratorModeType.Stereo);
      static MainWindowViewModel CreateMainWindowViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState();
         var mainWindowVM = new MainWindowViewModel(loadDefaultPlaylist: false)
         {
            WindowWidth = 900,
            WindowHeight = 600
         };
         mainWindowVM.PlaylistViewModel.AddNewTrack();
         mainWindowVM.PlaylistViewModel.TrackVMs[0].GeneratorMode = generatorModeType;
         return mainWindowVM;
      }
   }
   public class MainWindowViewModel : ViewModelBase
   {
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public PlotSampleViewModel PlotSampleViewModel { get; }
      public PlaylistViewModel PlaylistViewModel { get; }
      public AppState AppState { get; }

      public double WindowWidth { get => windowWidth; set => this.RaiseAndSetIfChanged(ref windowWidth, value); }
      public double WindowHeight { get => windowHeight; set => this.RaiseAndSetIfChanged(ref windowHeight, value); }

      // WindowPosition get, set is unlikely
      // Unable to bind to Window.Position https://github.com/AvaloniaUI/Avalonia/issues/3494
      // Add binding to Window.Position https://github.com/AvaloniaUI/Avalonia/pull/3521

      public string Title => $"Stimming Signal Generator {AppState.Version}";

      private double windowWidth;
      private double windowHeight;
      public MainWindowViewModel(bool loadDefaultPlaylist = true)
      {
         WindowWidth = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.WindowWidth, 900d);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.WindowWidth, () => WindowWidth.ToString())
            .DisposeWith(Disposables);

         WindowHeight = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.WindowHeight, 600d);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.WindowHeight, () => WindowHeight.ToString())
            .DisposeWith(Disposables);

         AppState = Locator.Current.GetService<AppState>();
         PlaylistViewModel = new PlaylistViewModel().DisposeWith(Disposables);
         if (loadDefaultPlaylist)
         {
            Observable.StartAsync(() => PlaylistViewModel.LoadDefaultAsync())
               .Subscribe()
               .DisposeWith(Disposables);
         }

         AudioPlayerViewModel =
            new AudioPlayerViewModel(PlaylistViewModel.FinalSample)
            .DisposeWith(Disposables);

         PlotSampleViewModel =
            new PlotSampleViewModel(AudioPlayerViewModel.PlotSampleProvider)
            .DisposeWith(Disposables);
      }

      public static void OpenGitHubPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator");
      public static void OpenGitHubReleasesPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator/releases");
      public static void OpenGitHubIssuesPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator/issues");
      public static void OpenDevelopmentChatroom() => OpenUrl("https://gitter.im/StimmingSignalGenerator/Development");
      public static void OpenWaveformChatroom() => OpenUrl("https://gitter.im/StimmingSignalGenerator/Waveform");

      private static void OpenUrl(string url)
      {
         System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo()
            {
               FileName = url,
               UseShellExecute = true
            }
         );
      }

   }
}
