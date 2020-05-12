using NAudio.Wave;
using StimmingSignalGenerator.Generators;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.POCOs
{
   class BasicSignal
   {
      public double Frequency { get; set; }
      public double ZeroCrossingPosition { get; set; }
      public double Gain { get; set; }
      public BasicSignalType Type { get; set; }
      public List<BasicSignal> AMSignals { get; set; }
      public List<BasicSignal> FMSignals { get; set; }
   }
}
