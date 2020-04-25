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
      public BasicSignalGeneratorViewModel LeftAMSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel LeftFMSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightAMSignalGeneratorVM { get; }
      public BasicSignalGeneratorViewModel RightFMSignalGeneratorVM { get; }
      public AudioPlayerViewModel AudioPlayerViewModel { get; }

      public MainWindowViewModel()
      {
         static (
         BasicSignalGeneratorViewModel Main,
         BasicSignalGeneratorViewModel AM,
         BasicSignalGeneratorViewModel FM)
         CreateSignalGeneratorVM(string namePrefix) => (
            new BasicSignalGeneratorViewModel()
            { Name = $"{namePrefix} Main Signal" },
            new BasicSignalGeneratorViewModel(
                  SignalSliderViewModel.AMSignalFreq)
            { Name = $"{namePrefix} AM Signal" },
            new BasicSignalGeneratorViewModel(
                  SignalSliderViewModel.FMSignalFreq,
                  SignalSliderViewModel.Vol(0.25))
            { Name = $"{namePrefix} FM Signal" }
            );

         (LeftSignalGeneratorVM, LeftAMSignalGeneratorVM, LeftFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Left");

         (RightSignalGeneratorVM, RightAMSignalGeneratorVM, RightFMSignalGeneratorVM) =
            CreateSignalGeneratorVM("Right");

         var leftSignelProvider =
            LeftSignalGeneratorVM.BasicSignalGenerator
            .AddAM(LeftAMSignalGeneratorVM.BasicSignalGenerator)
            .AddFM(LeftFMSignalGeneratorVM.BasicSignalGenerator);

         var rightSignelProvider =
            RightSignalGeneratorVM.BasicSignalGenerator
            .AddAM(RightAMSignalGeneratorVM.BasicSignalGenerator)
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
