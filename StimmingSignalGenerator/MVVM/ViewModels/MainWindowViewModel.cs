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
      public BasicSignalGeneratorViewModel BasicSignalGeneratorViewModelA { get; }
      public BasicSignalGeneratorViewModel BasicSignalGeneratorViewModelB { get; }
      public BasicSignalGeneratorViewModel BasicSignalGeneratorViewModelC { get; }
      public AudioPlayerViewModel AudioPlayerViewModel { get; }

      public MainWindowViewModel()
      {
         BasicSignalGeneratorViewModelA = 
            new BasicSignalGeneratorViewModel()
            { Name = "Main Signal" };
         BasicSignalGeneratorViewModelB = 
            new BasicSignalGeneratorViewModel(
               SignalSliderViewModel.AMSignalFreq)
            { Name = "AM Signal"};
         BasicSignalGeneratorViewModelC = 
            new BasicSignalGeneratorViewModel(
               SignalSliderViewModel.FMSignalFreq,
               SignalSliderViewModel.Vol(0.25))
            { Name = "FM Signal" };

         var finalProvider =
            BasicSignalGeneratorViewModelA.BasicSignalGenerator
            .AddAM(BasicSignalGeneratorViewModelB.BasicSignalGenerator)
            .AddFM(BasicSignalGeneratorViewModelC.BasicSignalGenerator);

         AudioPlayerViewModel = new AudioPlayerViewModel(finalProvider);
      }


   }
}
