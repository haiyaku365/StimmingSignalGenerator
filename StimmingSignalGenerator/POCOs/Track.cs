using System.Collections.Generic;

namespace StimmingSignalGenerator.POCOs
{
   public class Track
   {
      public string Name { get; set; }
      public List<MultiSignal> MultiSignals { get; set; }
      public List<ControlSlider> Volumes { get; set; }
      public double TimeSpanSecond { get; set; }
   }
}
