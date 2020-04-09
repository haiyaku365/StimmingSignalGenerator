using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.SignalGenerator
{
   public class SignalSourceControl
   {
      //TODO Make config serializable
      public SignalGeneratorType SignalType { get; set; } = SignalGeneratorType.Sin;
      public double Frequency { get; set; } = 500f;
      public double Volume { get; set; } = 0.5f;
   }
}
