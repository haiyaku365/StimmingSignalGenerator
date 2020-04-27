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
                  SignalSliderViewModel.AMSignalFreq)
            { Name = $"{namePrefix} AM1 Signal" },
            new BasicSignalGeneratorViewModel(
                  new SignalSliderViewModel(330,0,500,0.1,0.1,1), 
                  SignalSliderViewModel.Vol(0),
                  SignalSliderViewModel.Vol(0.15))
            { Name = $"{namePrefix} AM2 Signal" },
            new BasicSignalGeneratorViewModel(
                  SignalSliderViewModel.FMSignalFreq,
                  SignalSliderViewModel.Vol(0.25))
            { Name = $"{namePrefix} FM Signal" }
            );

         (LeftSignalGeneratorVM, LeftAM1SignalGeneratorVM, LeftAM2SignalGeneratorVM, LeftFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Left");

         (RightSignalGeneratorVM, RightAM1SignalGeneratorVM, RightAM2SignalGeneratorVM, RightFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Right");


      var leftSignelProvider =
            LeftSignalGeneratorVM.BasicSignalGenerator
            .AddAM(LeftAM1SignalGeneratorVM.BasicSignalGenerator)
            .AddAM(LeftAM2SignalGeneratorVM.BasicSignalGenerator)
            .AddFM(LeftFMSignalGeneratorVM.BasicSignalGenerator);

         var rightSignelProvider =
            RightSignalGeneratorVM.BasicSignalGenerator
            .AddAM(RightAM1SignalGeneratorVM.BasicSignalGenerator)
            .AddAM(RightAM2SignalGeneratorVM.BasicSignalGenerator)
            .AddFM(RightFMSignalGeneratorVM.BasicSignalGenerator);

         //combine ch
         var finalProvider = new MultiplexingSampleProvider(
            new[] {
               leftSignelProvider , //left ch
               rightSignelProvider },//right ch
            2
            );
         AudioPlayerViewModel = new AudioPlayerViewModel(finalProvider);
      }

      
   }
}
