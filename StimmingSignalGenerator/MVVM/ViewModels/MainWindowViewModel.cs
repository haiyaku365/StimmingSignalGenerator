using NAudio.Wave;
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
         PrepareAppState();
         var mainWindowVM = new MainWindowViewModel();
         mainWindowVM.PlaylistViewModel.TrackVMs[0].GeneratorMode = generatorModeType;
         return mainWindowVM;
      }
   }
   public class MainWindowViewModel : ViewModelBase, IDisposable
   {
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public PlotSampleViewModel PlotSampleViewModel { get; }
      public PlaylistViewModel PlaylistViewModel { get; }
      public AppState AppState { get; }
      public string Title => $"Stimming Signal Generator {AppState.Version}";

      public MainWindowViewModel()
      {
         AppState = Locator.Current.GetService<AppState>();
         PlaylistViewModel = new PlaylistViewModel();
         PlaylistViewModel.AddNewTrack();
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
