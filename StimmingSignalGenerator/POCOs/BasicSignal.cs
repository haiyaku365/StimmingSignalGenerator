using StimmingSignalGenerator.NAudio;
using System.Collections.Generic;

namespace StimmingSignalGenerator.POCOs
{
   public class BasicSignal
   {
      public BasicSignalType Type { get; set; }
      public ControlSlider Frequency { get; set; }
      public ControlSlider PhaseShift { get; set; }
      public ControlSlider Volume { get; set; }
      public ControlSlider ZeroCrossingPosition { get; set; }
      public List<BasicSignal> AMSignals { get; set; }
      public List<BasicSignal> FMSignals { get; set; }
      public List<BasicSignal> PMSignals { get; set; }
      public List<BasicSignal> ZMSignals { get; set; }
      public string FrequencySyncFrom { get; set; }
   }
}
