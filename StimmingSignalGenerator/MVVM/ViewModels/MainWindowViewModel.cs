using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class MainWindowViewModel : ViewModelBase
   {
      public SignalSourceControlViewModel SignalSourceControlViewModel { get; }
      public AudioPlayerViewModel AudioPlayerViewModel { get; }
      public MainWindowViewModel()
      {
         SignalSourceControlViewModel = new SignalSourceControlViewModel();
         AudioPlayerViewModel = new AudioPlayerViewModel(SignalSourceControlViewModel);

      }
   }
}
