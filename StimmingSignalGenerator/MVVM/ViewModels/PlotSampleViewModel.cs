using NAudio.Utils;
using NAudio.Wave;
using OxyPlot;
using ReactiveUI;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;

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

         var count = Constants.DefaultSampleRate / 8 * signal.WaveFormat.Channels;
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

      private bool isPlotEnable;
      private bool isHighDefinition;
      private readonly PlotSampleProvider plotSampleProvider;
      public PlotSampleViewModel(PlotSampleProvider plotSampleProvider)
      {
         this.plotSampleProvider = plotSampleProvider;
         PlotModel = plotSampleProvider.PlotModel;

         this.ObservableForProperty(x => x.IsPlotEnable)
            .Subscribe(x => plotSampleProvider.IsEnable = x.Value)
            .DisposeWith(Disposables);
         this.ObservableForProperty(x => x.IsHighDefinition)
            .Subscribe(x => plotSampleProvider.IsHighDefinition = x.Value)
            .DisposeWith(Disposables);
      }
   }
}
