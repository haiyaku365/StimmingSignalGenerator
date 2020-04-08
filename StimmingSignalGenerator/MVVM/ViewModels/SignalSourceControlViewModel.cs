using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.MVVM.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   class SignalSourceControlViewModel : ViewModelBase
   {
      private SignalGeneratorType signalType;
      private double frequency;
      private double volume;

      public SignalSourceControlViewModel()
      {
         SignalType = SignalGeneratorType.Sin;
         Frequency = 500f;
         Volume = 0.5f;
      }
      public SignalGeneratorType SignalType { get => signalType; 
         set => this.RaiseAndSetIfChanged(ref signalType, value); }
      public double Frequency { get => frequency; 
         set => this.RaiseAndSetIfChanged(ref frequency, value); }
      public double Volume { get => volume; 
         set => this.RaiseAndSetIfChanged(ref volume, value); }
   }
}
