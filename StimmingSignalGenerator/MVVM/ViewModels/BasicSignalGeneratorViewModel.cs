using NAudio.Wave.SampleProviders;
using ReactiveUI;
using StimmingSignalGenerator.SignalGenerator;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.MVVM.ViewModels
{
   public class BasicSignalGeneratorViewModel : ViewModelBase
   {
      public BasicSignalGenerator BasicSignalGenerator { get; set; }

      private SignalGeneratorType signalType;
      private double frequency;
      private double volume;

      public BasicSignalGeneratorViewModel()
      {
         BasicSignalGenerator = new BasicSignalGenerator();

         SignalType = SignalGeneratorType.Sin;
         Frequency = 440f;
         Volume = 1f;
      }
      public SignalGeneratorType SignalType
      {
         get => signalType;
         set
         {
            this.RaiseAndSetIfChanged(ref signalType, value);
            BasicSignalGenerator.Type = signalType;
         }
      }
      public double Frequency
      {
         get => frequency;
         set
         {
            this.RaiseAndSetIfChanged(ref frequency, value);
            BasicSignalGenerator.Frequency = frequency;
         }
      }
      public double Volume
      {
         get => volume;
         set
         {
            this.RaiseAndSetIfChanged(ref volume, value);
            BasicSignalGenerator.Gain = volume;
         }
      }
   }
}
