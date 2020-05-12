using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StimmingSignalGenerator.POCOs
{
   class MultiSignal
   {
      public double Gain { get; set; }
      public List<BasicSignal> BasicSignals { get; set; }
   }
}
