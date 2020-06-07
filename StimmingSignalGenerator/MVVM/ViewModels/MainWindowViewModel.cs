using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using ReactiveUI;
using Splat;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.NAudio.OxyPlot;
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
         PrepareAppState();
         var mainWindowVM = new MainWindowViewModel(loadDefaultPlaylist: false);
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
      public string Title => $"Stimming Signal Generator {AppState.Version}";

      public MainWindowViewModel(bool loadDefaultPlaylist = true)
      {
         AppState = Locator.Current.GetService<AppState>();
         PlaylistViewModel = new PlaylistViewModel().DisposeWith(Disposables);
         if (loadDefaultPlaylist)
         {
            Observable.StartAsync(() => PlaylistViewModel.LoadDefaultAsync())
               .Subscribe()
               .DisposeWith(Disposables);
         }

         PlotSampleViewModel =
            new PlotSampleViewModel(new PlotSampleProvider(PlaylistViewModel.FinalSample))
            .DisposeWith(Disposables);

         AppState
            .WhenAnyValue(x => x.IsHDPlot)
            .Subscribe(x => PlotSampleViewModel.IsHighDefinition = x)
            .DisposeWith(Disposables);
         AppState
            .WhenAnyValue(x => x.IsPlotEnable)
            .Subscribe(x => PlotSampleViewModel.IsPlotEnable = x)
            .DisposeWith(Disposables);
         AudioPlayerViewModel =
            new AudioPlayerViewModel(PlotSampleViewModel.SampleSignal)
            .DisposeWith(Disposables);
      }

      public static void OpenGitHubPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator");
      public static void OpenGitHubReleasesPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator/releases");
      public static void OpenGitHubIssuesPage() => OpenUrl("https://github.com/haiyaku365/StimmingSignalGenerator/issues");

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
