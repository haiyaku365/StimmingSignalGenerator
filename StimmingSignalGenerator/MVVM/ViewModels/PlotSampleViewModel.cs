using NAudio.Utils;
using NAudio.Wave;
using OxyPlot;
using ReactiveUI;
using StimmingSignalGenerator.Helper;
using StimmingSignalGenerator.NAudio;
using StimmingSignalGenerator.NAudio.OxyPlot;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using static StimmingSignalGenerator.NAudio.OxyPlot.PlotSampleProvider;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class DesignPlotSampleViewModel : DesignViewModelBase
   {
      public static PlotSampleViewModel MonoData => CreatePlotSampleViewModel(GeneratorModeType.Mono);
      public static PlotSampleViewModel StereoData => CreatePlotSampleViewModel(GeneratorModeType.Stereo);
      private static PlotSampleViewModel CreatePlotSampleViewModel(GeneratorModeType generatorModeType)
      {
         PrepareAppState();
         ISampleProvider signal = new BasicSignal();
         if (generatorModeType == GeneratorModeType.Stereo)
         {
            signal = signal.ToStereo(1, 0.5f);
         }
         var plotSignal = new PlotSampleProvider(signal);
         var plotVM = new PlotSampleViewModel(plotSignal);
         plotVM.IsPlotEnable = true;

         var count = Constants.Wave.DefaultSampleRate / 8 * signal.WaveFormat.Channels;
         float[] buffer = Array.Empty<float>();
         buffer = BufferHelpers.Ensure(buffer, count);
         plotVM.SampleSignal.Read(buffer, 0, count);

         return plotVM;
      }
   }
   public class PlotSampleViewModel : ViewModelBase
   {
      public PlotModel PlotModel { get; }
      public bool IsPlotEnable { get => isPlotEnable; set => this.RaiseAndSetIfChanged(ref isPlotEnable, value); }
      public bool IsHighDefinition { get => isHighDefinition; set => this.RaiseAndSetIfChanged(ref isHighDefinition, value); }
      public ISampleProvider SampleSignal => plotSampleProvider;
      public long InvalidatePlotPostedElapsedMilliseconds => invalidatePlotPostedElapsedMilliseconds.Value;
      public int MinPlotUpdateIntervalMilliseconds { get => minPlotUpdateIntervalMilliseconds; set => this.RaiseAndSetIfChanged(ref minPlotUpdateIntervalMilliseconds, value); }

      private bool isPlotEnable;
      private bool isHighDefinition;
      private readonly PlotSampleProvider plotSampleProvider;
      private readonly ObservableAsPropertyHelper<long> invalidatePlotPostedElapsedMilliseconds;
      private int minPlotUpdateIntervalMilliseconds;
      public PlotSampleViewModel(PlotSampleProvider plotSampleProvider)
      {
         this.plotSampleProvider = plotSampleProvider;
         PlotModel = plotSampleProvider.PlotModel;

         IsPlotEnable = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.IsPlotEnable, false);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.IsPlotEnable, () => IsPlotEnable.ToString())
            .DisposeWith(Disposables);
         IsHighDefinition = ConfigurationHelper.GetConfigOrDefault(Constants.ConfigKey.IsHighDefinition, false);
         ConfigurationHelper
            .AddUpdateAppSettingsOnDispose(Constants.ConfigKey.IsHighDefinition, () => IsHighDefinition.ToString())
            .DisposeWith(Disposables);

         this.WhenAnyValue(x => x.IsPlotEnable)
            .Subscribe(x => plotSampleProvider.IsEnable = x)
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.IsHighDefinition)
            .Subscribe(x => plotSampleProvider.IsHighDefinition = x)
            .DisposeWith(Disposables);

         Observable.FromEventPattern<OnInvalidatePlotPostedEventArgs>(
            h => plotSampleProvider.OnInvalidatePlotPosted += h,
            h => plotSampleProvider.OnInvalidatePlotPosted -= h)
            .Select(x => x.EventArgs.ElapsedMilliseconds)
            .ToProperty(this, nameof(InvalidatePlotPostedElapsedMilliseconds),
               out invalidatePlotPostedElapsedMilliseconds)
            .DisposeWith(Disposables);
         this.WhenAnyValue(x => x.MinPlotUpdateIntervalMilliseconds)
            .Subscribe(x => plotSampleProvider.MinPlotUpdateIntervalMilliseconds = x)
            .DisposeWith(Disposables);
      }
   }
}
