using NAudio.Wave;
using StimmingSignalGenerator.Generators;
using StimmingSignalGenerator.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace StimmingSignalGenerator.POCOs
{
   class BasicSignal
   {
      public BasicSignalType Type { get; set; }
      public ControlSlider Frequency { get; set; }
      public ControlSlider Volume { get; set; }
      public ControlSlider ZeroCrossingPosition { get; set; }
      public List<BasicSignal> AMSignals { get; set; }
      public List<BasicSignal> FMSignals { get; set; }
   }
}
