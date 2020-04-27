using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class MainWindowViewModel : ViewModelBase
   {
      public BasicSignalGeneratorViewModel LeftSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel LeftAM1SignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel LeftAM2SignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel LeftFMSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightAM1SignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightAM2SignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightFMSignalGeneratorVM { get; }
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
     
      public MainWindowViewModel()
      {
         static (
         BasicSignalGeneratorViewModel Main,
         BasicSignalGeneratorViewModel AM1,
         BasicSignalGeneratorViewModel AM2,
         BasicSignalGeneratorViewModel FM)
         CreateSignalGeneratorVM(string namePrefix) => (
            new BasicSignalGeneratorViewModel()
            { Name = $"{namePrefix} Main Signal" },
            new BasicSignalGeneratorViewModel(
                  ControlSliderViewModel.AMSignalFreq)
            { Name = $"{namePrefix} AM1 Signal" },
            new BasicSignalGeneratorViewModel(
                  new ControlSliderViewModel(335, 0, 500, 0.1, 0.1, 1),
                  ControlSliderViewModel.Vol(1),
                  ControlSliderViewModel.Vol(0.1))
            { Name = $"{namePrefix} AM2 Signal" },
            new BasicSignalGeneratorViewModel(
                  ControlSliderViewModel.FMSignalFreq,
                  ControlSliderViewModel.Vol(0))
            { Name = $"{namePrefix} FM Signal" }
            );

         (LeftSignalGeneratorVM, LeftAM1SignalGeneratorVM, LeftAM2SignalGeneratorVM, LeftFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Left");

         (RightSignalGeneratorVM, RightAM1SignalGeneratorVM, RightAM2SignalGeneratorVM, RightFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Right");

         /*
         AM Signal with gain bump
         https://www.desmos.com/calculator/ya9ayr9ylc
         f_{1}=1
         g_{0}=0.25
         y_{0}=g_{0}\sin\left(f_{1}\cdot2\pi x\right)
         y_{1}=y_{0}+1-g_{0}
         y_{2}=\frac{\left(y_{1}+1\right)}{2}
         y=\sin\left(20\cdot2\pi x\right)\cdot y_{2}\left\{-1<y<1\right\}
         */
         var leftSignal =
               LeftSignalGeneratorVM.BasicSignalGenerator
               .AddAM(LeftAM1SignalGeneratorVM.BasicSignalGenerator.Gain(g => g + 1 - (float)LeftAM1SignalGeneratorVM.Volume))
               .AddAM(LeftAM2SignalGeneratorVM.BasicSignalGenerator.Gain(g => g + 1 - (float)LeftAM2SignalGeneratorVM.Volume))
               .AddFM(LeftFMSignalGeneratorVM.BasicSignalGenerator);

         var rightSignal =
            RightSignalGeneratorVM.BasicSignalGenerator
            .AddAM(RightAM1SignalGeneratorVM.BasicSignalGenerator.Gain(g => g + 1 - (float)RightAM1SignalGeneratorVM.Volume))
            .AddAM(RightAM2SignalGeneratorVM.BasicSignalGenerator.Gain(g => g + 1 - (float)RightAM2SignalGeneratorVM.Volume))
            .AddFM(RightFMSignalGeneratorVM.BasicSignalGenerator);

         //combine ch
         var multiplexedSignal = new MultiplexingSampleProvider(
            new[] {
               leftSignal , //left ch
               rightSignal },//right ch
            2
            );

         AudioPlayerViewModel = new AudioPlayerViewModel(multiplexedSignal);
      }


   }
}
