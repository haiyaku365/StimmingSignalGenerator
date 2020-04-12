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
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public MainWindowViewModel()
      {
         BasicSignalGeneratorViewModelA = new BasicSignalGeneratorViewModel();
         BasicSignalGeneratorViewModelB = new BasicSignalGeneratorViewModel();

         var finalGen = new AmplitudeModulationGenerator(
            BasicSignalGeneratorViewModelA.BasicSignalGenerator, 
            BasicSignalGeneratorViewModelB.BasicSignalGenerator );

         AudioPlayerViewModel = new AudioPlayerViewModel(finalGen);

      }
   }
}
