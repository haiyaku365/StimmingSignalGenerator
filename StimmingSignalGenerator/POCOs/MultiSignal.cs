using System.Collections.Generic;

namespace StimmingSignalGenerator.POCOs
{
   public class MultiSignal
   {
      public ControlSlider Volume { get; set; }
      public List<BasicSignal> BasicSignals { get; set; }
   }
}
