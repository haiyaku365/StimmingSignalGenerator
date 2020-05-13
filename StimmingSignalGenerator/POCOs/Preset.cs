using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StimmingSignalGenerator.POCOs
{
   class Preset
   {
      public List<MultiSignal> MultiSignals { get; set; }
   }
}
